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
    public float expansionInterval = 5f;

    [Header("AI")]
    public int visionRadius = 2;

    // wiele oddzia³ów
    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();
    private readonly List<Vector3Int> tokenLastPositions = new();

    private Vector3Int spawnPos;
    private float timer;
    private bool initialized;

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

        timer = expansionInterval;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = expansionInterval;
            DoTurn();
        }
    }

    // ------------------------------------------------------------
    // TURA
    // ------------------------------------------------------------
    void DoTurn()
    {
        GainGoldForTurn();

        // jeœli mamy wystarczaj¹c¹ populacjê - rekrutuj
        TryCreateNewUnitFromPopulation();

        // ka¿dy token wykonuje 1 ruch na turê/interwa³
        // UWAGA: iterujemy od koñca, bo token mo¿e zostaæ zniszczony w walce
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

        // 1) krok obok: priorytet neutralne, a gdy brak neutralnych - wrogie (walka)
        if (TryChooseBestAdjacentStep(currentPos, lastPos, out var bestAdjacent))
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

        // 2) jeœli nie ma sensownego kroku obok - idŸ w kierunku najlepszego celu widocznego z terytorium 
        bool TryMoveTowardsBestVisibleTarget(Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
        {
            step = default;

            HashSet<Vector3Int> visibleSet = GetTerritoryVision(visionRadius);

            // 1) najpierw spróbuj znaleŸæ neutralny cel
            Vector3Int? bestNeutral = null;
            int bestNeutralDist = int.MaxValue;
            int bestNeutralScore = int.MinValue;

            // 2) jeœli nie ma neutralnych, szukamy wrogiego celu (do ataku)
            Vector3Int? bestEnemy = null;
            int bestEnemyDist = int.MaxValue;
            int bestEnemyScore = int.MinValue;

            foreach (var pos in visibleSet)
            {
                if (!map.TryGetCell(pos, out var cell)) continue;
                if (!cell.passable) continue;

                int owner = cell.ownerId;
                if (owner == botOwnerId) continue; // swoje nie jest celem

                int dist = HexDist(currentPos, pos);

                int score = 0;
                if (cell.hasMine) score += 1_000_000;
                score += cell.populationNumber;

                if (owner == 0)
                {
                    // neutralne: minimalny dystans, potem max score
                    if (dist < bestNeutralDist || (dist == bestNeutralDist && score > bestNeutralScore))
                    {
                        bestNeutralDist = dist;
                        bestNeutralScore = score;
                        bestNeutral = pos;
                    }
                }
                else
                {
                    // wrogie: minimalny dystans, potem max score
                    if (dist < bestEnemyDist || (dist == bestEnemyDist && score > bestEnemyScore))
                    {
                        bestEnemyDist = dist;
                        bestEnemyScore = score;
                        bestEnemy = pos;
                    }
                }
            }

            // wybieramy cel: neutralne jeœli istnieje, w przeciwnym razie wrogie
            Vector3Int? target = bestNeutral ?? bestEnemy;
            if (!target.HasValue) return false;

            // idŸ o 1 krok w stronê celu
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
        // LIMIT TOKENÓW
        if (tokens.Count >= maxArmyTokens)
            return;

        // ZA MA£O POPULACJI
        if (population < populationToCreateNewUnit)
            return;

        // koszt populacji
        population -= populationToCreateNewUnit;

        // nowy oddzia³ na spawnie
        int idx = SpawnToken(spawnPos, initialArmySize: newUnitArmySize);
        if (idx >= 0)
        {
            tokenPositions[idx] = spawnPos;
            tokenLastPositions[idx] = spawnPos;
        }

        // +100 do armii bazy
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

    // ------------------------------------------------------------
    // Krok obok: neutralne najpierw, a gdy brak neutralnych -> wrogie (walka)
    // ------------------------------------------------------------
    bool TryChooseBestAdjacentStep(Vector3Int currentPos, Vector3Int lastPos, out Vector3Int bestStep)
    {
        bestStep = default;

        var neighbours = map.GetNeighbours(currentPos);

        List<Vector3Int> neutral = new();
        List<Vector3Int> enemy = new();

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            int owner = map.GetOwnerId(n);
            if (owner == 0) neutral.Add(n);
            else if (owner != botOwnerId) enemy.Add(n);
        }

        if (neutral.Count > 0)
            return PickBestByMineThenPop(neutral, out bestStep);

        if (enemy.Count > 0)
            return PickBestByMineThenPop(enemy, out bestStep);

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
    // Marsz do najlepszego celu widocznego z terytorium (neutralne)
    // (gdy obok nie ma neutralnych)
    // ------------------------------------------------------------
    bool TryMoveTowardsBestVisibleTarget(Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;

        HashSet<Vector3Int> visibleSet = GetTerritoryVision(visionRadius);

        Vector3Int? bestTarget = null;
        int bestTargetDist = int.MaxValue;
        int bestTargetScore = int.MinValue;

        foreach (var pos in visibleSet)
        {
            if (!map.TryGetCell(pos, out var cell)) continue;
            if (!cell.passable) continue;
            if (cell.ownerId != 0) continue; // chcemy tylko neutralne

            int dist = HexDist(currentPos, pos);

            int score = 0;
            if (cell.hasMine) score += 1_000_000;
            score += cell.populationNumber;

            // najpierw minimalny dystans, a przy remisie wiêkszy score
            if (dist < bestTargetDist || (dist == bestTargetDist && score > bestTargetScore))
            {
                bestTargetDist = dist;
                bestTargetScore = score;
                bestTarget = pos;
            }
        }

        if (!bestTarget.HasValue) return false;

        if (!map.TryGetNextStep(currentPos, bestTarget.Value, out var nextStep))
            return false;

        // unikaj cofania jeœli siê da
        if (nextStep == lastPos)
        {
            var neighbours = map.GetNeighbours(currentPos);
            int bestDist = HexDist(nextStep, bestTarget.Value);
            Vector3Int bestAlt = nextStep;

            foreach (var n in neighbours)
            {
                if (!map.IsPassableLand(n)) continue;
                if (n == lastPos) continue;

                int d = HexDist(n, bestTarget.Value);
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

        // swoje pole -> tylko ruch
        if (cell.ownerId == botOwnerId)
            return true;

        // neutralne -> normalne przejêcie
        if (cell.ownerId == 0)
        {
            CaptureCell(targetPos);
            return true;
        }

        // wrogie -> walka z armi¹ pola
        return ResolveBattleOnEnemyTile(unitIndex, targetPos, cell);
    }

    bool ResolveBattleOnEnemyTile(int unitIndex, Vector3Int pos, HexCell cell)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        int attacker = tokens[unitIndex].armySize;
        int defender = Mathf.Max(0, cell.army);

        // jeœli pole wrogie ale bez armii -> darmowe przejêcie
        if (defender <= 0)
        {
            ConquerEnemyTile(pos, cell);
            return true;
        }

        // wygrywa wiêksza armia
        if (attacker <= defender)
        {
            // przegrana: token ginie
            KillToken(unitIndex);
            return false;
        }

        // wygrana: strata 80-120% armii pokonanego
        float mult = Random.Range(winLossMin, winLossMax);
        int loss = Mathf.RoundToInt(defender * mult);
        tokens[unitIndex].armySize -= loss;

        // przejmujemy pole
        ConquerEnemyTile(pos, cell);

        // jeœli po stratach <=0 -> ginie
        if (tokens[unitIndex].armySize <= 0)
        {
            KillToken(unitIndex);
            return false;
        }

        return true;
    }

    void ConquerEnemyTile(Vector3Int pos, HexCell cell)
    {
        int previousOwner = cell.ownerId;

        // przejêcie (kolor/Tile)
        map.SetOwnerAndTile(pos, botOwnerId, botTile);

        // garnizon na zdobytym polu (Twoja mechanika)
        cell.army = populationPerCapture;

        // kopalnia -> nasz licznik +1
        // (nie odejmujemy poprzedniemu botowi, bo na razie nie obs³ugujemy przeciwnych tokenów)
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
    // Capture (zmienia tile + zbiera populacjê + ustawia garnizon + liczy kopalnie)
    // ------------------------------------------------------------
    void CaptureCell(Vector3Int cellPos)
    {
        if (!map.TryGetCell(cellPos, out HexCell cell))
            return;

        if (cell.ownerId != 0)
            return;

        map.SetOwnerAndTile(cellPos, botOwnerId, botTile);

        // bot zbiera populacjê: pop - 10
        int gainedPopulation = Mathf.Max(0, cell.populationNumber - populationPerCapture);
        population += gainedPopulation;

        // pole dostaje garnizon
        cell.army = populationPerCapture;

        // kopalnia -> zwiêksz licznik
        if (cell.hasMine)
            ownedMineCount++;
    }

    // ------------------------------------------------------------
    // Gold income
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

            // + zasiêg od niego (po przechodnich)
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
