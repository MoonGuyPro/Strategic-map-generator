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
        for (int i = 0; i < tokens.Count; i++)
        {
            DoUnitStep(i);
        }
    }

    void DoUnitStep(int unitIndex)
    {
        Vector3Int currentPos = tokenPositions[unitIndex];
        Vector3Int lastPos = tokenLastPositions[unitIndex];

        // 1) jeœli mogê przej¹æ neutralne pole obok - zrób to (kopalnia/populacja)
        if (TryChooseBestCaptureStep(currentPos, lastPos, out var captureStep))
        {
            CaptureCell(captureStep);

            tokenLastPositions[unitIndex] = currentPos;
            tokenPositions[unitIndex] = captureStep;
            UpdateToken(unitIndex, captureStep);
            return;
        }

        // 2) jeœli nie ma neutralnych obok - idŸ w kierunku najlepszego celu widocznego z terytorium
        if (TryMoveTowardsBestVisibleTarget(currentPos, lastPos, out var moveStep))
        {
            tokenLastPositions[unitIndex] = currentPos;
            tokenPositions[unitIndex] = moveStep;
            UpdateToken(unitIndex, moveStep);
            return;
        }

        // 3) ostateczny fallback (losowo)
        if (TryMoveFallbackRandom(currentPos, lastPos, out var randomStep))
        {
            tokenLastPositions[unitIndex] = currentPos;
            tokenPositions[unitIndex] = randomStep;
            UpdateToken(unitIndex, randomStep);
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
    // Wybór neutralnego kroku obok (capture-now)
    // ------------------------------------------------------------
    bool TryChooseBestCaptureStep(Vector3Int currentPos, Vector3Int lastPos, out Vector3Int bestStep)
    {
        bestStep = default;

        // widzenie terytorialne (¿eby wiedzieæ o kopalniach w promieniu 2 od terytorium)
        HashSet<Vector3Int> visibleSet = GetTerritoryVision(visionRadius);

        // minesInSight = kopalnie widoczne z terytorium
        List<Vector3Int> minesInSight = new();
        foreach (var v in visibleSet)
        {
            if (!map.TryGetCell(v, out var c)) continue;
            if (!c.passable) continue;
            if (c.hasMine && c.ownerId != botOwnerId)
                minesInSight.Add(v);
        }

        var neighbours = map.GetNeighbours(currentPos);

        int bestScore = int.MinValue;
        bool found = false;

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            // przejmujemy tylko neutralne w 1 kroku
            if (map.GetOwnerId(n) != 0) continue;

            if (!map.TryGetCell(n, out var cell)) continue;

            int score = 0;

            // 1) jeœli to neutralna kopalnia -> absolutny priorytet
            if (cell.hasMine) score += 1_000_000;

            // 2) jeœli widzimy kopalnie w promieniu 2 od terytorium:
            //    premiuj kroki, które przybli¿aj¹ do kopalni
            if (minesInSight.Count > 0)
            {
                int bestMineDistNow = int.MaxValue;
                int bestMineDistAfter = int.MaxValue;

                foreach (var m in minesInSight)
                {
                    int dNow = HexDist(currentPos, m);
                    int dAfter = HexDist(n, m);

                    if (dNow < bestMineDistNow) bestMineDistNow = dNow;
                    if (dAfter < bestMineDistAfter) bestMineDistAfter = dAfter;
                }

                if (bestMineDistAfter < bestMineDistNow)
                    score += 100_000;
            }

            // 3) populacja jako drugi priorytet
            score += cell.populationNumber;

            if (!found || score > bestScore)
            {
                bestScore = score;
                bestStep = n;
                found = true;
            }
        }

        return found;
    }

    // ------------------------------------------------------------
    // Marsz do najlepszego celu widocznego z terytorium
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
