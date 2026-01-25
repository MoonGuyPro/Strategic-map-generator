using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility AI (scoring) dla tokenów bota.
/// - Ta klasa NIC nie rusza na mapie (nie wykonuje TryEnterCell / walki).
/// - Ona tylko WYBIERA "najlepszy krok" (jeden s¹siad) dla danego tokena.
/// Egzekucjê ruchu zostawiasz w BotController (¿eby nie dublowaæ logiki walki/capture).
/// </summary>
public class UtilityAIController : MonoBehaviour
{
    [Header("Referencje")]
    [Tooltip("Bot, dla którego liczymy decyzje (ten sam obiekt co BotController).")]
    public BotController self;

    [Tooltip("Przeciwny bot (opcjonalnie, potrzebny do regu³y ataku tokenów).")]
    public BotController enemy;

    [Tooltip("Mapa")]
    public HexMapGenerator map;

    [Header("Ruch / widocznoœæ")]
    [Tooltip("Zasiêg")]
    public int moveRadius = 1;

    [Header("Wagi (priorytety decyzji)")]
    public Weights weights = new Weights();



    [System.Serializable]
    public class Weights
    {
        [Header("1) WejdŸ na kopalniê w zasiêgu ruchu")]
        public float mineBase = 1_000_000f;
        public float minePopFactor = 50f; // ile punktów za 1 populacji na kopalni

        [Header("2) WejdŸ na neutralne pole obok (max populacja)")]
        public float neutralAdjBase = 200_000f;
        public float neutralAdjPopFactor = 1_000f;

        [Header("3) IdŸ w kierunku najlepszego NEUTRALNEGO pola granicz¹cego z terytorium")]
        public float neutralBorderDirBase = 150_000f;
        public float neutralBorderDirPopFactor = 800f;
        public float neutralBorderDirDistancePenalty = 5_000f; // kara za dalszy cel (¿eby preferowaæ bli¿sze)

        [Header("4) Atakuj bazê przeciwnika w zasiêgu ruchu (jeœli masz wiêksz¹ armiê)")]
        public float attackEnemyBase = 900_000f;

        [Header("5) Atakuj token przeciwnika w zasiêgu ruchu (jeœli masz wiêksz¹ armiê)")]
        public float attackEnemyToken = 700_000f;

        [Header("6) Atakuj pole przeciwnika w zasiêgu ruchu (max populacja)")]
        public float enemyAdjBase = 120_000f;
        public float enemyAdjPopFactor = 1_000f;
        public float enemyAdjMineBonus = 300_000f; // jeœli wrogie pole ma kopalniê

        [Header("7) IdŸ w kierunku najlepszego WROGIEGO pola granicz¹cego z terytorium")]
        public float enemyBorderDirBase = 100_000f;
        public float enemyBorderDirPopFactor = 700f;
        public float enemyBorderDirDistancePenalty = 5_000f;

        [Header("Ogólne kary / bonusy")]
        public float backtrackPenalty = 50_000f; // kara za cofniêcie na lastPos
        public float invalidMovePenalty = 10_000_000f; // jeœli krok jest z³y, zbijamy score
    }

    // ------------------------------------------------------------
    // API: zwraca najlepszy KROK (s¹siad) dla tokena
    // ------------------------------------------------------------

    /// <summary>
    /// Zwraca najlepszy krok (jedno pole s¹siednie), zgodnie z list¹ regu³ (1-7) poprzez scoring.
    /// currentPos i lastPos bierzesz z BotController tokenPositions/tokenLastPositions.
    /// </summary>
    public bool TryGetBestStep(int tokenIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int bestStep)
    {
        bestStep = default;

        if (self == null) return false;
        if (map == null) map = self.map;
        if (map == null) return false;

        // ruch o 1 = s¹siedzi
        List<Vector3Int> neighbours = map.GetNeighbours(currentPos);
        if (neighbours == null || neighbours.Count == 0) return false;

        // Precompute: cele "kierunkowe" (3 i 7)
        bool hasNeutralBorderTarget = TryFindBestBorderTarget(ownerId: 0, currentPos, out var bestNeutralBorderTarget, out var neutralBorderTargetPop, out var neutralBorderTargetDist);
        bool hasEnemyBorderTarget = TryFindBestEnemyBorderTarget(currentPos, out var bestEnemyBorderTarget, out var enemyBorderTargetPop, out var enemyBorderTargetDist);

        // Precompute: pozycja bazy przeciwnika (4)
        Vector3Int enemyBasePos = GetEnemyBasePos();

        // armia atakuj¹cego tokena
        int myArmy = SafeGetTokenArmy(self, tokenIndex);

        bool found = false;
        float bestScore = float.NegativeInfinity;


        foreach (var step in neighbours)
        {
            float score = ScoreStep(tokenIndex, myArmy, currentPos, lastPos, step,
                hasNeutralBorderTarget, bestNeutralBorderTarget, neutralBorderTargetPop, neutralBorderTargetDist,
                hasEnemyBorderTarget, bestEnemyBorderTarget, enemyBorderTargetPop, enemyBorderTargetDist,
                enemyBasePos);

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                bestStep = step;
            }
        }

        return found;
    }

    // ------------------------------------------------------------
    // Scoring (serce Utility AI)
    // ------------------------------------------------------------

    float ScoreStep(
        int tokenIndex,
        int myArmy,
        Vector3Int currentPos,
        Vector3Int lastPos,
        Vector3Int step,
        bool hasNeutralBorderTarget,
        Vector3Int bestNeutralBorderTarget,
        int neutralBorderTargetPop,
        int neutralBorderTargetDist,
        bool hasEnemyBorderTarget,
        Vector3Int bestEnemyBorderTarget,
        int enemyBorderTargetPop,
        int enemyBorderTargetDist,
        Vector3Int enemyBasePos
    )
    {
        // podstawowe sprawdzenia
        if (!map.IsPassableLand(step))
            return -weights.invalidMovePenalty;

        if (!map.TryGetCell(step, out HexCell cell))
            return -weights.invalidMovePenalty;

        int owner = cell.ownerId;
        int pop = cell.populationNumber;
        bool isMine = cell.hasMine;

        float score = 0f;

        // kara za cofanie
        if (step == lastPos)
            score -= weights.backtrackPenalty;

        // ------------------------------------------------------------------
        // 4) baza przeciwnika w zasiêgu ruchu i mamy wiêksz¹ armiê ni¿ pole
        // ------------------------------------------------------------------
        // Uwaga: "pole bazy" rozpoznajemy po tym, ¿e step == enemyBasePos.
        if (step == enemyBasePos && owner != self.botOwnerId && owner != 0)
        {
            int defender = Mathf.Max(0, cell.army);
            if (myArmy > defender)
            {
                score += weights.attackEnemyBase;
            }
            else
            {
                // jeœli nie mamy przewagi - mocno zbijamy, ¿eby bot nie wchodzi³ i nie gin¹³ bez sensu
                score -= weights.attackEnemyBase;
            }
        }

        // ------------------------------------------------------------------
        // 5) token przeciwnika w zasiêgu ruchu i mamy przewagê armii
        // ------------------------------------------------------------------
        // To dzia³a tylko jeœli przypiszesz "enemy" w Inspectorze.
        if (enemy != null)
        {
            int enemyTokenIndex = enemy.FindTokenIndexAt(step);
            if (enemyTokenIndex != -1)
            {
                int enemyArmy = SafeGetTokenArmy(enemy, enemyTokenIndex);
                if (myArmy > enemyArmy)
                    score += weights.attackEnemyToken;
                else
                    score -= weights.attackEnemyToken; // nie atakuj silniejszego
            }
        }

        // ------------------------------------------------------------------
        // 1) kopalnia w zasiêgu ruchu (neutralna lub wroga)
        // ------------------------------------------------------------------
        if (isMine && owner != self.botOwnerId)
        {
            score += weights.mineBase;
            score += pop * weights.minePopFactor;
        }

        // ------------------------------------------------------------------
        // 2) neutralne pole obok (max populacja)
        // ------------------------------------------------------------------
        if (owner == 0)
        {
            score += weights.neutralAdjBase;
            score += pop * weights.neutralAdjPopFactor;
        }

        // ------------------------------------------------------------------
        // 6) wrogie pole w zasiêgu ruchu (max populacja)
        // ------------------------------------------------------------------
        if (owner != 0 && owner != self.botOwnerId)
        {
            score += weights.enemyAdjBase;
            score += pop * weights.enemyAdjPopFactor;

            if (isMine) score += weights.enemyAdjMineBonus;
        }

        // ------------------------------------------------------------------
        // 3) kierunek: najlepsze neutralne pole granicz¹ce z terytorium
        // ------------------------------------------------------------------
        // Je¿eli mamy taki cel -> premiujemy KROK, który jest "nastêpnym krokiem" w jego stronê.
        if (hasNeutralBorderTarget)
        {
            if (TryIsNextStepTowards(currentPos, bestNeutralBorderTarget, step))
            {
                score += weights.neutralBorderDirBase;
                score += neutralBorderTargetPop * weights.neutralBorderDirPopFactor;
                score -= neutralBorderTargetDist * weights.neutralBorderDirDistancePenalty;
            }
        }

        // ------------------------------------------------------------------
        // 7) kierunek: najlepsze wrogie pole granicz¹ce z terytorium
        // ------------------------------------------------------------------
        if (hasEnemyBorderTarget)
        {
            if (TryIsNextStepTowards(currentPos, bestEnemyBorderTarget, step))
            {
                score += weights.enemyBorderDirBase;
                score += enemyBorderTargetPop * weights.enemyBorderDirPopFactor;
                score -= enemyBorderTargetDist * weights.enemyBorderDirDistancePenalty;
            }
        }

        return score;
    }

    // ------------------------------------------------------------
    // Helpery: cele na granicy terytorium
    // ------------------------------------------------------------

    /// <summary>
    /// ZnajdŸ najlepsze neutralne pole, które graniczy z terytorium bota (czyli ma s¹siada nale¿¹cego do bota).
    /// Najpierw minimalny dystans od tokena, a przy remisie wiêksza populacja.
    /// </summary>
    bool TryFindBestBorderTarget(int ownerId, Vector3Int from, out Vector3Int bestTarget, out int bestPop, out int bestDist)
    {
        bestTarget = default;
        bestPop = 0;
        bestDist = int.MaxValue;

        bool found = false;

        foreach (var c in map.DebugCells)
        {
            if (!c.passable) continue;
            if (c.ownerId != ownerId) continue;

            // czy to pole graniczy z NASZYM terytorium?
            if (!BordersOurTerritory(c.coord))
                continue;

            int dist = HexDist(from, c.coord);
            int pop = c.populationNumber;

            if (!found || dist < bestDist || (dist == bestDist && pop > bestPop))
            {
                found = true;
                bestDist = dist;
                bestPop = pop;
                bestTarget = c.coord;
            }
        }

        return found;
    }

    /// <summary>
    /// Jak wy¿ej, ale dla WROGICH pól granicz¹cych z naszym terytorium.
    /// </summary>
    bool TryFindBestEnemyBorderTarget(Vector3Int from, out Vector3Int bestTarget, out int bestPop, out int bestDist)
    {
        bestTarget = default;
        bestPop = 0;
        bestDist = int.MaxValue;

        bool found = false;

        foreach (var c in map.DebugCells)
        {
            if (!c.passable) continue;
            if (c.ownerId == 0) continue;
            if (c.ownerId == self.botOwnerId) continue;

            if (!BordersOurTerritory(c.coord))
                continue;

            int dist = HexDist(from, c.coord);
            int pop = c.populationNumber;

            if (!found || dist < bestDist || (dist == bestDist && pop > bestPop))
            {
                found = true;
                bestDist = dist;
                bestPop = pop;
                bestTarget = c.coord;
            }
        }

        return found;
    }

    bool BordersOurTerritory(Vector3Int cellPos)
    {
        var n = map.GetNeighbours(cellPos);
        foreach (var nb in n)
        {
            if (!map.TryGetCell(nb, out var nbCell)) continue;
            if (nbCell.ownerId == self.botOwnerId)
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------
    // Helpery: "czy to jest nastêpny krok w stronê celu"
    // ------------------------------------------------------------
    bool TryIsNextStepTowards(Vector3Int from, Vector3Int target, Vector3Int candidateStep)
    {
        // Korzystamy z Twojej mapowej pathfind funkcji.
        // Jeœli TryGetNextStep zwróci krok == candidateStep -> premiujemy.
        if (!map.TryGetNextStep(from, target, out var next))
            return false;

        return next == candidateStep;
    }

    // ------------------------------------------------------------
    // Enemy base pos (spawn przeciwnika)
    // ------------------------------------------------------------
    Vector3Int GetEnemyBasePos()
    {
        // Jeœli bot jest "spawnNumber == 1", to wróg ma spawnPosPlayer2 itd.
        // To jest zgodne z Twoj¹ logik¹ w BotController.
        if (self.spawnNumber == 2)
            return map.spawnPosPlayer1;

        return map.spawnPosPlayer2;
    }

    // ------------------------------------------------------------
    // Safe get token army
    // ------------------------------------------------------------
    int SafeGetTokenArmy(BotController bc, int idx)
    {
        if (bc == null) return 0;
        if (idx < 0 || idx >= bc.TokenCount) return 0;
        var t = bc.GetToken(idx);
        return t != null ? t.armySize : 0;
    }

    // ------------------------------------------------------------
    // Hex distance (kopiujemy z BotController ¿eby UtilityAI nie zale¿a³ od private)
    // ------------------------------------------------------------
    int HexDist(Vector3Int a, Vector3Int b)
    {
        var ac = OddRToCube(a);
        var bc = OddRToCube(b);
        return (Mathf.Abs(ac.x - bc.x) +
                Mathf.Abs(ac.y - bc.y) +
                Mathf.Abs(ac.z - bc.z)) / 2;
    }

    Vector3Int OddRToCube(Vector3Int h)
    {
        int x = h.x - (h.y - (h.y & 1)) / 2;
        int z = h.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }
}
