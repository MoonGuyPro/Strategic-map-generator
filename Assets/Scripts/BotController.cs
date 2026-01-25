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
    public UtilityAIController utilityAI;


    // wiele oddzia³ów
    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();
    private readonly List<Vector3Int> tokenLastPositions = new();

    private Vector3Int spawnPos;
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

        // NOWA LOGIKA: Utility AI wybiera najlepszy krok (jedno pole s¹siednie)
        if (utilityAI != null && utilityAI.TryGetBestStep(unitIndex, currentPos, lastPos, out var bestStep))
        {
            bool aliveAndMoved = TryEnterCell(unitIndex, bestStep);
            if (aliveAndMoved && unitIndex < tokens.Count) // token móg³ umrzeæ
            {
                tokenLastPositions[unitIndex] = currentPos;
                tokenPositions[unitIndex] = bestStep;
                UpdateToken(unitIndex, bestStep);
            }
            return;
        }

        // Fallback awaryjny - jakby utilityAI nie by³o przypisane
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

    // ---- PUBLIC API dla BotTurnManager (token vs token) ----

    public int TokenCount => tokens.Count;

    public Vector3Int GetTokenPos(int index) => tokenPositions[index];

    public ArmyToken GetToken(int index) => tokens[index];

    // Zwraca indeks tokena stoj¹cego na danym polu lub -1
    public int FindTokenIndexAt(Vector3Int cellPos)
    {
        for (int i = 0; i < tokenPositions.Count; i++)
            if (tokenPositions[i] == cellPos)
                return i;
        return -1;
    }

    // Zabicie tokena (u¿ywa Twojej istniej¹cej logiki)
    public void KillTokenPublic(int tokenIndex)
    {
        KillToken(tokenIndex);
    }

    // Przejêcie pola po walce token vs token (¿eby kolor siê zmieni³)
    public void ClaimTileAfterTokenBattle(Vector3Int pos)
    {
        if (!map.TryGetCell(pos, out var cell)) return;

        int previousOwner = cell.ownerId;

        map.SetOwnerAndTile(pos, botOwnerId, botTile);

        // garnizon jak w Twojej logice terytorium
        cell.army = populationPerCapture;

        // kopalnia: +1 dla zwyciêzcy, -1 dla przegranego (opcjonalnie, ale sensowne)
        if (cell.hasMine && previousOwner != botOwnerId)
        {
            ownedMineCount++;
            // UWAGA: jeœli chcesz odejmowaæ przegranemu, to zrobimy to w TurnManagerze
            // bo BotController nie zna przeciwnika.
        }
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
