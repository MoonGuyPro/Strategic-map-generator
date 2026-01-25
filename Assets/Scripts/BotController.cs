using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BotController : MonoBehaviour
{
    [Header("Referencje")]
    public HexMapGenerator map;
    public TileBase botTile;

    [Header("Populacja")]
    public int population;
    public int populationPerCapture = 10; // ile zabieramy z pola (wg Twojej logiki: bot dostaje pop-10)

    [Header("Rekrutacja oddzia³ów")]
    public int populationToCreateNewUnit = 600;   // próg (inspektor)
    public int newUnitArmySize = 500;             // armySize nowego tokena
    public int baseArmyBonus = 100;               // ile idzie do armii bazy przy rekrutacji
    public int maxArmyTokens = 5;

    [Header("Z³oto")]
    public int gold = 0;
    public int goldPerIntervalByBase = 70;
    public int goldGainedByMine = 30;
    public int ownedMineCount = 0;

    [Header("Armia (wizualizacja)")]
    public ArmyToken armyTokenPrefab;
    public Sprite armySprite;

    [Header("Walka")]
    [Range(0f, 2f)] public float winLossMin = 0.8f;  // 80%
    [Range(0f, 2f)] public float winLossMax = 1.2f;  // 120%

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;

    [Header("AI")]
    public int visionRadius = 1;

    // wiele oddzia³ów
    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();
    private readonly List<Vector3Int> tokenLastPositions = new();

    private Vector3Int spawnPos;
    private bool initialized;

    public Vector3Int SpawnPos => spawnPos;

    private System.Collections.IEnumerator Start()
    {
        if (map == null || botTile == null)
        {
            Debug.LogError("BotController: brak map lub botTile!");
            yield break;
        }

        yield return null; // czekamy a¿ mapa siê wygeneruje

        spawnPos = (spawnNumber == 2) ? map.spawnPosPlayer2 : map.spawnPosPlayer1;

        // Start: oznacz spawn jako nasze terytorium (nie liczymy tego jako "capture")
        map.SetOwnerAndTile(spawnPos, botOwnerId, botTile);

        // Start: pierwszy oddzia³
        int tokenIndex = SpawnToken(spawnPos, initialArmySize: 300);
        if (tokenIndex >= 0)
        {
            tokenPositions[tokenIndex] = spawnPos;
            tokenLastPositions[tokenIndex] = spawnPos;
        }

        initialized = true;
    }

    // ------------------------------------------------------------
    // TURA
    // ------------------------------------------------------------
    public void TakeTurn()
    {
        if (!initialized) return;
        DoTurn();
    }

    void DoTurn()
    {
        GainGoldForTurn();
        TryCreateNewUnitFromPopulation();

        // ka¿dy token wykonuje 1 ruch na turê/interwa³
        // iterujemy od koñca, bo token mo¿e zostaæ zniszczony w walce o pole
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            DoUnitStep(i);
        }
    }

    void DoUnitStep(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return;

        Vector3Int currentPos = tokenPositions[unitIndex];
        Vector3Int lastPos = tokenLastPositions[unitIndex];

        // 1) krok obok: neutralne, a gdy brak neutralnych - wrogie (ale TYLKO jeœli do wygrania)
        if (TryChooseBestAdjacentStep(unitIndex, currentPos, lastPos, out var bestAdjacent))
        {
            bool aliveAndMoved = TryEnterCell(unitIndex, bestAdjacent);
            if (aliveAndMoved && unitIndex < tokens.Count) // token móg³ umrzeæ
            {
                tokenLastPositions[unitIndex] = currentPos;
                tokenPositions[unitIndex] = bestAdjacent;
                UpdateToken(unitIndex, bestAdjacent);
            }
            return;
        }

        // 2) jeœli nie ma sensownego kroku obok - idŸ w kierunku najlepszego celu widocznego
        if (TryMoveTowardsBestVisibleTarget(unitIndex, currentPos, lastPos, out var moveStep))
        {
            bool aliveAndMoved = TryEnterCell(unitIndex, moveStep);
            if (aliveAndMoved && unitIndex < tokens.Count)
            {
                tokenLastPositions[unitIndex] = currentPos;
                tokenPositions[unitIndex] = moveStep;
                UpdateToken(unitIndex, moveStep);
            }
            return;
        }

        // 3) ostateczny fallback (losowo)
        if (TryMoveFallbackRandom(currentPos, lastPos, out var randomStep))
        {
            bool aliveAndMoved = TryEnterCell(unitIndex, randomStep);
            if (aliveAndMoved && unitIndex < tokens.Count)
            {
                tokenLastPositions[unitIndex] = currentPos;
                tokenPositions[unitIndex] = randomStep;
                UpdateToken(unitIndex, randomStep);
            }
        }
    }

    // ------------------------------------------------------------
    // Rekrutacja nowego oddzia³u
    // ------------------------------------------------------------
    void TryCreateNewUnitFromPopulation()
    {
        if (tokens.Count >= maxArmyTokens) return;
        if (population < populationToCreateNewUnit) return;

        population -= populationToCreateNewUnit;

        int idx = SpawnToken(spawnPos, initialArmySize: newUnitArmySize);
        if (idx >= 0)
        {
            tokenPositions[idx] = spawnPos;
            tokenLastPositions[idx] = spawnPos;
        }

        if (map.TryGetCell(spawnPos, out var baseCell))
        {
            baseCell.army += baseArmyBonus;
        }
    }

    // ------------------------------------------------------------
    // Tokeny
    // ------------------------------------------------------------
    int SpawnToken(Vector3Int cell, int initialArmySize)
    {
        if (armyTokenPrefab == null || armySprite == null)
        {
            Debug.LogWarning("BotController: brak armyTokenPrefab lub armySprite - pionek nie bêdzie widoczny.");
            return -1;
        }

        ArmyToken token = Instantiate(armyTokenPrefab, transform);
        token.Init(armySprite);
        token.armySize = initialArmySize;
        token.TeleportToCell(map.tilemap, cell);

        tokens.Add(token);
        tokenPositions.Add(cell);
        tokenLastPositions.Add(cell);

        return tokens.Count - 1;
    }

    void UpdateToken(int index, Vector3Int cell)
    {
        if (index < 0 || index >= tokens.Count) return;
        tokens[index].TeleportToCell(map.tilemap, cell);
    }

    // ---- PUBLIC API ----
    public int TokenCount => tokens.Count;
    public Vector3Int GetTokenPos(int index) => tokenPositions[index];
    public ArmyToken GetToken(int index) => tokens[index];

    public List<Vector3Int> GetAllTokenPositionsCopy()
    {
        return new List<Vector3Int>(tokenPositions);
    }

    public int FindTokenIndexAt(Vector3Int cellPos)
    {
        for (int i = 0; i < tokenPositions.Count; i++)
            if (tokenPositions[i] == cellPos)
                return i;
        return -1;
    }

    public void KillTokenPublic(int tokenIndex) => KillToken(tokenIndex);

    // ------------------------------------------------------------
    // Token vs Token – ROZSTRZYGNIÊCIE KOLIZJI (przeniesione z TurnManagera)
    // TurnManager powinien tylko zawo³aæ: attacker.ResolveCollisionsWith(other)
    // ------------------------------------------------------------
    public void ResolveCollisionsWith(BotController other)
    {
        if (other == null) return;

        // iterujemy od koñca, bo bêdziemy usuwaæ tokeny
        for (int i = TokenCount - 1; i >= 0; i--)
        {
            if (i >= TokenCount) continue; // safety po ewentualnych removach

            Vector3Int pos = GetTokenPos(i);

            int j = other.FindTokenIndexAt(pos);
            if (j < 0) continue;

            // indeksy s¹ aktualne na moment pobrania
            ArmyToken aTok = GetToken(i);
            ArmyToken bTok = other.GetToken(j);

            // remis: obaj gin¹
            if (aTok.armySize == bTok.armySize)
            {
                KillToken(i);
                other.KillToken(j);
                continue;
            }

            // zwyciêzca = wiêksza armia
            bool thisWins = aTok.armySize > bTok.armySize;
            BotController winner = thisWins ? this : other;
            BotController loser = thisWins ? other : this;

            int winnerIndex = thisWins ? i : j;
            int loserIndex = thisWins ? j : i;

            // UWAGA: po usuniêciu przegranego indeksy w loserze siê zmieni¹, ale winner jest w innym obiekcie
            ArmyToken wTok = winner.GetToken(winnerIndex);
            ArmyToken lTok = loser.GetToken(loserIndex);

            int loserArmy = lTok.armySize;

            float mult = Random.Range(winner.winLossMin, winner.winLossMax);
            int loss = Mathf.RoundToInt(loserArmy * mult);

            // kopalnie: jeœli pole ma kopalniê i zmienia w³aœciciela, popraw licznik obu botów
            int previousOwner = 0;
            bool hasMine = false;
            if (winner.map != null && winner.map.TryGetCell(pos, out var cell))
            {
                previousOwner = cell.ownerId;
                hasMine = cell.hasMine;
            }

            // zabij przegranego
            loser.KillToken(loserIndex);

            // odejmij straty zwyciêzcy
            wTok.armySize -= loss;

            if (wTok.armySize <= 0)
            {
                winner.KillToken(winnerIndex);
                continue;
            }

            // przejêcie pola
            winner.ClaimTileAfterTokenBattle(pos);

            // korekta kopalni (jeœli faktycznie zmieni³o ownera)
            if (hasMine && previousOwner != 0 && previousOwner != winner.botOwnerId)
            {
                // winner dosta³ +1 wewn¹trz ClaimTileAfterTokenBattle
                // loser powinien straciæ 1 (jeœli to by³ jego mineCount)
                loser.ownedMineCount = Mathf.Max(0, loser.ownedMineCount - 1);
            }
        }
    }

    // przejêcie pola po walce token vs token (¿eby kolor siê zmieni³)
    public void ClaimTileAfterTokenBattle(Vector3Int pos)
    {
        if (!map.TryGetCell(pos, out var cell)) return;

        int previousOwner = cell.ownerId;

        map.SetOwnerAndTile(pos, botOwnerId, botTile);

        // garnizon jak w Twojej logice terytorium
        cell.army = populationPerCapture;

        // kopalnia: +1 dla zwyciêzcy
        if (cell.hasMine && previousOwner != botOwnerId)
        {
            ownedMineCount++;
        }
    }

    // ------------------------------------------------------------
    // Krok obok: neutralne najpierw, a gdy brak neutralnych -> wrogie (ALE: tylko atakowalne)
    // ------------------------------------------------------------
    bool TryChooseBestAdjacentStep(int unitIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int bestStep)
    {
        bestStep = default;

        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        var neighbours = map.GetNeighbours(currentPos);

        List<Vector3Int> neutral = new();
        List<Vector3Int> enemyAttackable = new();

        int attackerArmy = tokens[unitIndex].armySize;

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            int owner = map.GetOwnerId(n);

            if (owner == 0)
            {
                neutral.Add(n);
            }
            else if (owner != botOwnerId)
            {
                if (!map.TryGetCell(n, out var enemyCell)) continue;
                int defender = Mathf.Max(0, enemyCell.army);

                // tylko jeœli to ma sens: darmowe przejêcie albo realna wygrana
                if (defender <= 0 || attackerArmy > defender)
                    enemyAttackable.Add(n);
            }
        }

        if (neutral.Count > 0)
            return PickBestByMineThenPop(neutral, out bestStep);

        if (enemyAttackable.Count > 0)
            return PickBestByMineThenPop(enemyAttackable, out bestStep);

        return false;
    }

    bool PickBestByMineThenPop(List<Vector3Int> candidates, out Vector3Int bestStep)
    {
        bestStep = default;
        bool found = false;
        int bestScore = int.MinValue;

        foreach (var p in candidates)
        {
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;

            int score = 0;
            if (cell.hasMine) score += 1_000_000;
            score += cell.populationNumber;

            if (!found || score > bestScore)
            {
                bestScore = score;
                bestStep = p;
                found = true;
            }
        }

        return found;
    }

    // ------------------------------------------------------------
    // Marsz do najlepszego celu widocznego (neutralne preferowane)
    // DODATKOWO: nie wybieraj wrogiego celu, jeœli w zasiêgu jest nie-do-wygrania (minimalnie)
    // ------------------------------------------------------------
    bool TryMoveTowardsBestVisibleTarget(int unitIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;

        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        HashSet<Vector3Int> visibleSet = GetTerritoryVision(visionRadius);

        int attackerArmy = tokens[unitIndex].armySize;

        Vector3Int? bestNeutral = null;
        int bestNeutralDist = int.MaxValue;
        int bestNeutralScore = int.MinValue;

        Vector3Int? bestEnemy = null;
        int bestEnemyDist = int.MaxValue;
        int bestEnemyScore = int.MinValue;

        foreach (var pos in visibleSet)
        {
            if (!map.TryGetCell(pos, out var cell)) continue;
            if (!cell.passable) continue;

            int owner = cell.ownerId;
            if (owner == botOwnerId) continue;

            int dist = HexDist(currentPos, pos);

            int score = 0;
            if (cell.hasMine) score += 1_000_000;
            score += cell.populationNumber;

            if (owner == 0)
            {
                if (dist < bestNeutralDist || (dist == bestNeutralDist && score > bestNeutralScore))
                {
                    bestNeutralDist = dist;
                    bestNeutralScore = score;
                    bestNeutral = pos;
                }
            }
            else
            {
                // minimalny filtr: nie idŸ na wrogie, którego na pewno nie wygrasz (armia pola)
                int defender = Mathf.Max(0, cell.army);
                if (!(defender <= 0 || attackerArmy > defender))
                    continue;

                if (dist < bestEnemyDist || (dist == bestEnemyDist && score > bestEnemyScore))
                {
                    bestEnemyDist = dist;
                    bestEnemyScore = score;
                    bestEnemy = pos;
                }
            }
        }

        Vector3Int? target = bestNeutral ?? bestEnemy;
        if (!target.HasValue) return false;

        if (!map.TryGetNextStep(currentPos, target.Value, out var nextStep))
            return false;

        // unikaj cofania jeœli siê da
        if (nextStep == lastPos)
        {
            var neighbours = map.GetNeighbours(currentPos);
            int bestDist = HexDist(nextStep, target.Value);
            Vector3Int bestAlt = nextStep;

            foreach (var n in neighbours)
            {
                if (!map.IsPassableLand(n)) continue;
                if (n == lastPos) continue;

                int d = HexDist(n, target.Value);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestAlt = n;
                }
            }

            nextStep = bestAlt;
        }

        step = nextStep;
        return true;
    }

    bool TryMoveFallbackRandom(Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;

        var neighbours = map.GetNeighbours(currentPos);
        List<Vector3Int> passable = new();

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;
            passable.Add(n);
        }

        if (passable.Count == 0) return false;

        step = passable[Random.Range(0, passable.Count)];
        return true;
    }

    // ------------------------------------------------------------
    // WEJŒCIE NA POLE: neutralne -> capture, wrogie -> walka, swoje -> ruch
    // ------------------------------------------------------------
    bool TryEnterCell(int unitIndex, Vector3Int targetPos)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        if (!map.TryGetCell(targetPos, out HexCell cell))
            return false;

        if (cell.ownerId == botOwnerId)
            return true;

        if (cell.ownerId == 0)
        {
            CaptureCell(targetPos);
            return true;
        }

        return ResolveBattleOnEnemyTile(unitIndex, targetPos, cell);
    }

    bool ResolveBattleOnEnemyTile(int unitIndex, Vector3Int pos, HexCell cell)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        int attacker = tokens[unitIndex].armySize;
        int defender = Mathf.Max(0, cell.army);

        if (defender <= 0)
        {
            ConquerEnemyTile(pos, cell);
            return true;
        }

        if (attacker <= defender)
        {
            KillToken(unitIndex);
            return false;
        }

        float mult = Random.Range(winLossMin, winLossMax);
        int loss = Mathf.RoundToInt(defender * mult);
        tokens[unitIndex].armySize -= loss;

        // Jeœli po stratach <=0 -> ginie (i NIE przejmuje pola)
        if (tokens[unitIndex].armySize <= 0)
        {
            KillToken(unitIndex);
            return false;
        }

        ConquerEnemyTile(pos, cell);
        return true;
    }

    void ConquerEnemyTile(Vector3Int pos, HexCell cell)
    {
        int previousOwner = cell.ownerId;

        map.SetOwnerAndTile(pos, botOwnerId, botTile);
        cell.army = populationPerCapture;

        if (cell.hasMine && previousOwner != botOwnerId)
            ownedMineCount++;
    }

    void KillToken(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return;

        Destroy(tokens[unitIndex].gameObject);

        tokens.RemoveAt(unitIndex);
        tokenPositions.RemoveAt(unitIndex);
        tokenLastPositions.RemoveAt(unitIndex);
    }

    // ------------------------------------------------------------
    // Capture
    // ------------------------------------------------------------
    void CaptureCell(Vector3Int cellPos)
    {
        if (!map.TryGetCell(cellPos, out HexCell cell))
            return;

        if (cell.ownerId != 0)
            return;

        map.SetOwnerAndTile(cellPos, botOwnerId, botTile);

        int gainedPopulation = Mathf.Max(0, cell.populationNumber - populationPerCapture);
        population += gainedPopulation;

        cell.army = populationPerCapture;

        if (cell.hasMine)
            ownedMineCount++;
    }

    // ------------------------------------------------------------
    // Gold
    // ------------------------------------------------------------
    void GainGoldForTurn()
    {
        int income = goldPerIntervalByBase + (ownedMineCount * goldGainedByMine);
        gold += income;
    }

    // ------------------------------------------------------------
    // Vision od terytorium
    // ------------------------------------------------------------
    HashSet<Vector3Int> GetTerritoryVision(int radius)
    {
        HashSet<Vector3Int> visible = new HashSet<Vector3Int>();

        foreach (var cell in map.DebugCells)
        {
            if (cell.ownerId != botOwnerId) continue;

            visible.Add(cell.coord);

            List<Vector3Int> around = map.GetCellsInRange(cell.coord, radius);
            foreach (var a in around)
                visible.Add(a);
        }

        return visible;
    }

    // ------------------------------------------------------------
    // Hex distance helpers
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
