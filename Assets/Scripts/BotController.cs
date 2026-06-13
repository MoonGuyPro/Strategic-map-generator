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
    public int populationPerCapture = 10;

    [Header("Rekrutacja oddzia³ów")]
    public int populationToCreateNewUnit = 600;
    public int newUnitArmySize = 500;
    public int baseArmyBonus = 100;
    public int maxArmyTokens = 5;

    [Header("Z³oto")]
    public int gold = 0;
    public int goldPerIntervalByBase = 70;
    public int goldGainedByMine = 30;
    public int ownedMineCount = 0;

    [Header("Armia (wizualizacja)")]
    public ArmyToken armyTokenPrefab;
    public Sprite armySprite;

    [Header("Limity armii")]
    public int baseArmyCap = 600;                 // limit armii na polu bazy
    public int tokenArmyCapStart = 500;           // pocz¹tkowy limit armii w tokenie
    public int tokenArmyCapIncreasePerInterval = 100; // +100 co 15 tur

    [Header("Baza")]
    public GameObject basePrefab;

    [Header("Walka")]
    [Range(0f, 2f)] public float winLossMin = 0.8f;
    [Range(0f, 2f)] public float winLossMax = 1.2f;

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;

    [Header("AI")]
    [Tooltip("Zasiêg widocznoœci od granicy terytorium/tokena.")]
    public int visionRadius = 1;

    [Header("AI - przeciwnik")]
    [Tooltip("Ustaw w Inspectorze albo w BotTurnManager: botA.enemyBot=botB i odwrotnie.")]
    public BotController enemyBot;

    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();
    private readonly List<Vector3Int> tokenLastPositions = new();

    private Vector3Int spawnPos;
    private bool initialized;
    private int tokenArmyCap;

    // ------------------------------------------------------------
    // Populacja pasywna co X tur
    // ------------------------------------------------------------
    [Header("Populacja - pasywny przyrost")]
    public int populationIncomeIntervalTurns = 15;
    [Range(0f, 1f)] public float populationIncomePercent = 0.10f;

    private int turnCounter = 0;

    private GameObject spawnedBase;
    public Vector3Int SpawnPos => spawnPos;

    private System.Collections.IEnumerator Start()
    {
        if (map == null || botTile == null)
        {
            Debug.LogError("BotController: brak map lub botTile!");
            yield break;
        }

        // Czekamy a¿ generator mapy skoñczy
        while (!map.IsGenerated)
            yield return null;

        spawnPos = (spawnNumber == 2) ? map.spawnPosPlayer2 : map.spawnPosPlayer1;

        // baza ma byæ nasza na start
        map.SetOwnerAndTile(spawnPos, botOwnerId, botTile);

        tokenArmyCap = tokenArmyCapStart;

        if (map.TryGetCell(spawnPos, out var baseCell))
            baseCell.army = Mathf.Clamp(baseCell.army, 0, baseArmyCap);

        SpawnBase();

        // token ma wystartowaæ w bazie
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
        if (map.GetOwnerId(spawnPos) != botOwnerId) return;
        DoTurn();
    }

    void DoTurn()
    {
        turnCounter++;

        // co X tur: +pop +zwiêksz limit tokenów +spróbuj dobiæ tokeny do nowego limitu (z populacji)
        if (populationIncomeIntervalTurns > 0 && (turnCounter % populationIncomeIntervalTurns) == 0)
        {
            AddPopulationFromOwnedTiles();
            IncreaseTokenArmyCap();
            RefillAllTokensUpToCapFromPopulation();
        }

        GainGoldForTurn();
        TryCreateNewUnitFromPopulation();

        for (int i = tokens.Count - 1; i >= 0; i--)
            DoUnitStep(i);
    }

    void AddPopulationFromOwnedTiles()
    {
        if (map == null) return;
        int sum = 0;

        foreach (var cell in map.DebugCells)
        {
            if (cell.ownerId != botOwnerId) continue;
            if (!cell.passable || cell.isWater) continue;
            sum += Mathf.Max(0, cell.populationNumber);
        }

        int gained = Mathf.FloorToInt(sum * populationIncomePercent);
        if (gained <= 0) return;

        population += gained;

        // opcjonalnie debug
        Debug.LogWarning($"Bot[{botOwnerId}] +{gained} pop (10% z sumy {sum}) co {populationIncomeIntervalTurns} tur. Pop={population}");
    }

    void IncreaseTokenArmyCap()
    {
        tokenArmyCap += tokenArmyCapIncreasePerInterval;
        //ogranicz maksymalny cap np. do 2000:
        tokenArmyCap = Mathf.Min(tokenArmyCap, 1000);

        Debug.LogWarning($"Bot[{botOwnerId}] tokenArmyCap zwiï¿½kszony do {tokenArmyCap}");
    }

    void RefillAllTokensUpToCapFromPopulation()
    {
        for (int i = 0; i < tokens.Count; i++)
            RefillTokenUpToCapFromPopulation(i);
    }

    void RefillTokenUpToCapFromPopulation(int tokenIndex)
    {
        if (tokenIndex < 0 || tokenIndex >= tokens.Count) return;
        if (population <= 0) return;

        var tok = tokens[tokenIndex];
        if (tok == null) return;

        int need = Mathf.Max(0, tokenArmyCap - tok.armySize);
        if (need <= 0) return;

        int add = Mathf.Min(need, population);
        tok.armySize += add;
        population -= add;
    }

    void DoUnitStep(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return;

        Vector3Int currentPos = tokenPositions[unitIndex];
        Vector3Int lastPos = tokenLastPositions[unitIndex];

        if (TryChooseStepByPriorities(unitIndex, currentPos, lastPos, out var step))
        {
            bool aliveAndMoved = TryEnterCell(unitIndex, step);
            if (aliveAndMoved && unitIndex < tokens.Count)
            {
                tokenLastPositions[unitIndex] = currentPos;
                tokenPositions[unitIndex] = step;
                UpdateToken(unitIndex, step);
            }
            return;
        }

        // Fallback losowy
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

    // ============================================================
    // METODA POMOCNICZA DLA WIDOCZNOŒCI
    // ============================================================
    HashSet<Vector3Int> GetVisibleTiles(Vector3Int tokenPos)
    {
        HashSet<Vector3Int> visible = new HashSet<Vector3Int>();

        // 1. Wizja samego tokena
        visible.Add(tokenPos);
        foreach (var n in map.GetNeighbours(tokenPos))
            if (map.IsPassableLand(n)) visible.Add(n);

        // 2. Wizja terytorialna (nasze pola + s¹siedzi)
        foreach (var cell in map.DebugCells)
        {
            if (cell.ownerId == botOwnerId)
            {
                visible.Add(cell.coord);
                foreach (var n in map.GetNeighbours(cell.coord))
                {
                    if (map.IsPassableLand(n)) visible.Add(n);
                }
            }
        }
        return visible;
    }

    bool IsWinnable(Vector3Int pos, int attackerArmy)
    {
        if (!map.TryGetCell(pos, out var cell)) return false;

        int totalDefense = Mathf.Max(0, cell.army);

        // Dodaj si³ê pionka przeciwnika, jeœli tam stoi
        if (enemyBot != null)
        {
            int enemyTokenIdx = enemyBot.FindTokenIndexAt(pos);
            if (enemyTokenIdx != -1)
                totalDefense += enemyBot.GetToken(enemyTokenIdx).armySize;
        }

        // >= pozwala na atak na równych sobie (¿eby oba zginê³y i przerwa³y pêtlê)
        return attackerArmy >= totalDefense;
    }

    // ============================================================
    // PRIORYTETY 1–7 (Zgodne z GDD)
    // ============================================================
    bool TryChooseStepByPriorities(int unitIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        int attackerArmy = tokens[unitIndex].armySize;
        HashSet<Vector3Int> visibleTiles = GetVisibleTiles(currentPos);

        // 1) Kopalnia w zasiêgu
        if (TryPickMineTarget(visibleTiles, currentPos, attackerArmy, out var t1))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Kopalnia");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t1, out step);
        }

        // 2) Neutralne pole o najwiêkszej populacji
        if (TryPickNeutralMaxPop(visibleTiles, currentPos, out var t2))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Pole neutralne w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t2, out step);
        }

        // 3) Neutralne pole graniczne
        if (TryPickNeutralBorderMaxPop(currentPos, out var t3))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Neutral border");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t3, out step);
        }

        // 4) Baza przeciwnika (tylko jeœli oddzia³ ma wystarczaj¹c¹ armiê)
        if (TryPickEnemyBase(visibleTiles, currentPos, attackerArmy, out var t4))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Baza przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t4, out step);
        }

        // 5) Oddzia³ przeciwnika w zasiêgu
        if (TryPickEnemyToken(visibleTiles, currentPos, attackerArmy, out var t5))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Token przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t5, out step);
        }

        // 6) Wrogie pole o najwiêkszej populacji (jeœli mo¿liwa wygrana)
        if (TryPickEnemyMaxPop(visibleTiles, currentPos, attackerArmy, out var t6))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Atak na pole przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t6, out step);
        }

        // 7) Wrogie pole graniczne
        if (TryPickEnemyBorderMaxPop(currentPos, attackerArmy, out var t7))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Atak na pole przeciwnika border z najwiêksz¹ populacj¹");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t7, out step);
        }

        return false;
    }

    // ---------- Priorytet 1 ----------
    bool TryPickMineTarget(HashSet<Vector3Int> visible, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        bool found = false;
        int bestDist = int.MaxValue;
        int bestPop = int.MinValue;

        foreach (var p in visible)
        {
            if (!map.TryGetCell(p, out var cell) || !cell.passable || !cell.hasMine) continue;
            if (cell.ownerId == botOwnerId) continue;

            // jeœli wróg - musi byæ do wygrania
            if (cell.ownerId != 0 && !IsWinnable(p, attackerArmy)) continue;

            int dist = HexDist(currentPos, p);
            int pop = cell.populationNumber;

            if (!found || dist < bestDist || (dist == bestDist && pop > bestPop))
            {
                found = true; bestDist = dist; bestPop = pop; target = p;
            }
        }
        return found;
    }

    // ---------- Priorytet 2 ----------
    bool TryPickNeutralMaxPop(HashSet<Vector3Int> visible, Vector3Int currentPos, out Vector3Int target)
    {
        target = default;
        bool found = false;
        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in visible)
        {
            if (!map.TryGetCell(p, out var cell) || !cell.passable || cell.ownerId != 0) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true; bestPop = pop; bestDist = dist; target = p;
            }
        }
        return found;
    }

    // ---------- Priorytet 3 ----------
    bool TryPickNeutralBorderMaxPop(Vector3Int currentPos, out Vector3Int target)
    {
        target = default;
        var borderNeutrals = GetBorderCells(ownerIdFilter: 0);
        if (borderNeutrals.Count == 0) return false;

        bool found = false;
        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in borderNeutrals)
        {
            if (!map.TryGetCell(p, out var cell) || !cell.passable) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true; bestPop = pop; bestDist = dist; target = p;
            }
        }
        return found;
    }

    // ---------- Priorytet 4 ----------
    bool TryPickEnemyBase(HashSet<Vector3Int> visible, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        if (enemyBot == null) return false;

        Vector3Int enemyBase = enemyBot.SpawnPos;
        if (!visible.Contains(enemyBase)) return false;

        if (!map.TryGetCell(enemyBase, out var cell) || !cell.passable) return false;
        if (cell.ownerId == botOwnerId) return false;

        if (!IsWinnable(enemyBase, attackerArmy)) return false;

        target = enemyBase;
        return true;
    }

    // ---------- Priorytet 5 ----------
    bool TryPickEnemyToken(HashSet<Vector3Int> visible, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        if (enemyBot == null) return false;

        bool found = false;
        int bestDist = int.MaxValue;
        int bestEnemyArmy = int.MinValue;

        for (int i = 0; i < enemyBot.TokenCount; i++)
        {
            Vector3Int pos = enemyBot.GetTokenPos(i);
            if (!visible.Contains(pos)) continue;

            ArmyToken tok = enemyBot.GetToken(i);
            if (tok == null) continue;

            // Atakujemy, jeœli jesteœmy silniejsi LUB RÓWNI (gin¹ oba)
            if (attackerArmy < tok.armySize) continue;

            int dist = HexDist(currentPos, pos);

            if (!found || dist < bestDist || (dist == bestDist && tok.armySize > bestEnemyArmy))
            {
                found = true; bestDist = dist; bestEnemyArmy = tok.armySize; target = pos;
            }
        }
        return found;
    }

    // ---------- Priorytet 6 ----------
    bool TryPickEnemyMaxPop(HashSet<Vector3Int> visible, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        bool found = false;
        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in visible)
        {
            if (!map.TryGetCell(p, out var cell) || !cell.passable) continue;
            if (cell.ownerId == 0 || cell.ownerId == botOwnerId) continue;

            if (!IsWinnable(p, attackerArmy)) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true; bestPop = pop; bestDist = dist; target = p;
            }
        }
        return found;
    }

    // ---------- Priorytet 7 ----------
    bool TryPickEnemyBorderMaxPop(Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        var borderEnemies = GetBorderCells(ownerIdFilter: -1);
        if (borderEnemies.Count == 0) return false;

        bool found = false;
        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in borderEnemies)
        {
            if (!map.TryGetCell(p, out var cell) || !cell.passable) continue;
            if (!IsWinnable(p, attackerArmy)) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true; bestPop = pop; bestDist = dist; target = p;
            }
        }
        return found;
    }

    // ------------------------------------------------------------
    // KROK w stronê targetu + unikanie cofki + unikanie wejœcia na nie-do-wygrania enemy tile
    // ------------------------------------------------------------
    bool TryStepTowardsTarget(Vector3Int currentPos, Vector3Int lastPos, int attackerArmy, Vector3Int target, out Vector3Int step)
    {
        step = default;

        if (target == currentPos) return false;
        if (!map.TryGetNextStep(currentPos, target, out var nextStep))
            return false;

        if (nextStep == lastPos)
            nextStep = PickBestNeighbourTowardsTarget(currentPos, lastPos, target);

        if (!IsStepSafeForTileBattle(nextStep, attackerArmy))
        {
            var alt = PickBestNeighbourTowardsTarget(currentPos, lastPos, target, requireSafeEnemyStep: true, attackerArmy: attackerArmy);
            if (alt != nextStep)
                nextStep = alt;
        }

        if (!map.IsPassableLand(nextStep)) return false;

        step = nextStep;
        return true;
    }

    bool IsStepSafeForTileBattle(Vector3Int pos, int attackerArmy)
    {
        if (!map.TryGetCell(pos, out var cell)) return true;
        if (!cell.passable) return false;

        if (cell.ownerId != 0 && cell.ownerId != botOwnerId)
        {
            return IsWinnable(pos, attackerArmy);
        }
        return true;
    }

    Vector3Int PickBestNeighbourTowardsTarget(Vector3Int currentPos, Vector3Int lastPos, Vector3Int target, bool requireSafeEnemyStep = false, int attackerArmy = 0)
    {
        var neighbours = map.GetNeighbours(currentPos);
        Vector3Int best = currentPos;
        bool found = false;
        int bestDist = int.MaxValue;

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            if (requireSafeEnemyStep && !IsStepSafeForTileBattle(n, attackerArmy))
                continue;

            int d = HexDist(n, target);
            if (!found || d < bestDist)
            {
                found = true;
                bestDist = d;
                best = n;
            }
        }
        return found ? best : currentPos;
    }

    // ------------------------------------------------------------
    // Border cells
    // ------------------------------------------------------------
    List<Vector3Int> GetBorderCells(int ownerIdFilter)
    {
        HashSet<Vector3Int> result = new HashSet<Vector3Int>();

        foreach (var cell in map.DebugCells)
        {
            if (cell.ownerId != botOwnerId) continue;

            foreach (var n in map.GetNeighbours(cell.coord))
            {
                if (!map.IsPassableLand(n)) continue;

                int owner = map.GetOwnerId(n);

                if (ownerIdFilter == 0 && owner == 0)
                    result.Add(n);
                else if (ownerIdFilter == -1 && owner != 0 && owner != botOwnerId)
                    result.Add(n);
            }
        }
        return new List<Vector3Int>(result);
    }

    // ------------------------------------------------------------
    // Rekrutacja
    // ------------------------------------------------------------
    Vector3Int GetSpawnCellForNewToken()
    {
        if (FindTokenIndexAt(spawnPos) == -1)
            return spawnPos;

        var neighbours = map.GetNeighbours(spawnPos);
        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (FindTokenIndexAt(n) != -1) continue;
            return n;
        }
        return spawnPos;
    }

    void TryCreateNewUnitFromPopulation()
    {
        if (map.GetOwnerId(spawnPos) != botOwnerId) return;

        while (tokens.Count < maxArmyTokens && population >= populationToCreateNewUnit)
        {
            population -= populationToCreateNewUnit;

            Vector3Int spawnCell = GetSpawnCellForNewToken();

            int idx = SpawnToken(spawnCell, initialArmySize: 0);
            if (idx >= 0)
            {
                tokenPositions[idx] = spawnCell;
                tokenLastPositions[idx] = spawnCell;
                RefillTokenUpToCapFromPopulation(idx);
            }

            if (map.TryGetCell(spawnPos, out var baseCell))
                baseCell.army = Mathf.Min(baseArmyCap, baseCell.army + baseArmyBonus);
        }
    }

    // ------------------------------------------------------------
    // Tokeny
    // ------------------------------------------------------------
    int SpawnToken(Vector3Int cell, int initialArmySize)
    {
        if (armyTokenPrefab == null || armySprite == null)
        {
            Debug.LogWarning("BotController: brak armyTokenPrefab lub armySprite.");
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
    public List<Vector3Int> GetAllTokenPositionsCopy() => new List<Vector3Int>(tokenPositions);

    public int FindTokenIndexAt(Vector3Int cellPos)
    {
        for (int i = 0; i < tokenPositions.Count; i++)
            if (tokenPositions[i] == cellPos)
                return i;
        return -1;
    }

    public void KillTokenPublic(int tokenIndex) => KillToken(tokenIndex);

    // ------------------------------------------------------------
    // Token vs Token
    // ------------------------------------------------------------
    public void ResolveCollisionsWith(BotController other)
    {
        if (other == null) return;

        for (int i = TokenCount - 1; i >= 0; i--)
        {
            if (i >= TokenCount) continue;

            Vector3Int pos = GetTokenPos(i);
            int j = other.FindTokenIndexAt(pos);
            if (j < 0) continue;

            ArmyToken aTok = GetToken(i);
            ArmyToken bTok = other.GetToken(j);

            if (aTok.armySize == bTok.armySize)
            {
                KillToken(i);
                other.KillToken(j);
                continue;
            }

            bool thisWins = aTok.armySize > bTok.armySize;
            BotController winner = thisWins ? this : other;
            BotController loser = thisWins ? other : this;

            int winnerIndex = thisWins ? i : j;
            int loserIndex = thisWins ? j : i;

            ArmyToken wTok = winner.GetToken(winnerIndex);
            ArmyToken lTok = loser.GetToken(loserIndex);

            int loserArmy = lTok.armySize;

            float mult = Random.Range(winner.winLossMin, winner.winLossMax);
            int loss = Mathf.RoundToInt(loserArmy * mult);

            int previousOwner = 0;
            bool hasMine = false;
            if (winner.map != null && winner.map.TryGetCell(pos, out var cell))
            {
                previousOwner = cell.ownerId;
                hasMine = cell.hasMine;
            }

            loser.KillToken(loserIndex);
            wTok.armySize -= loss;

            if (wTok.armySize <= 0)
            {
                winner.KillToken(winnerIndex);
                continue;
            }

            winner.ClaimTileAfterTokenBattle(pos);

            if (hasMine && previousOwner != 0 && previousOwner != winner.botOwnerId)
            {
                loser.ownedMineCount = Mathf.Max(0, loser.ownedMineCount - 1);
            }
        }
    }

    public void ClaimTileAfterTokenBattle(Vector3Int pos)
    {
        if (!map.TryGetCell(pos, out var cell)) return;

        int previousOwner = cell.ownerId;

        map.SetOwnerAndTile(pos, botOwnerId, botTile);
        cell.army = populationPerCapture;

        if (cell.hasMine && previousOwner != botOwnerId)
            ownedMineCount++;
    }

    // ------------------------------------------------------------
    // WEJŒCIE NA POLE
    // ------------------------------------------------------------
    bool TryEnterCell(int unitIndex, Vector3Int targetPos)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;
        if (!map.TryGetCell(targetPos, out HexCell cell)) return false;

        if (cell.ownerId == botOwnerId) return true;

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
        if (!map.TryGetCell(cellPos, out HexCell cell)) return;
        if (cell.ownerId != 0) return;

        map.SetOwnerAndTile(cellPos, botOwnerId, botTile);

        int gainedPopulation = Mathf.Max(0, cell.populationNumber - populationPerCapture);
        population += gainedPopulation;
        cell.army = populationPerCapture;

        if (cell.hasMine) ownedMineCount++;
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
    // Fallback random
    // ------------------------------------------------------------
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
    // Hex helpers
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

    void SpawnBase()
    {
        if (spawnedBase != null) return;

        if (basePrefab == null)
        {
            Debug.LogError($"BotController[{botOwnerId}]: basePrefab NIE JEST PRZYPISANY!");
            return;
        }

        Vector3 worldPos = map.tilemap.GetCellCenterWorld(spawnPos);
        spawnedBase = Instantiate(basePrefab, worldPos, Quaternion.identity, transform);
        spawnedBase.name = $"Base_Bot_{botOwnerId}";
    }
}