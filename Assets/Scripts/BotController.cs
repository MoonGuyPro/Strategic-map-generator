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

    [Header("Rekrutacja oddzia��w")]
    public int populationToCreateNewUnit = 600;
    public int baseArmyBonus = 100;
    public int maxArmyTokens = 5;

    /*[Header("Z�oto")]
    public int gold = 0;
    public int goldPerIntervalByBase = 70;
    public int goldGainedByMine = 30;
    public int ownedMineCount = 0;*/

    [Header("Armia (wizualizacja)")]
    public ArmyToken armyTokenPrefab;
    public Sprite armySprite;

    [Header("Limity armii")]
    public int tokenArmyCapIncreasePerInterval = 100; // +100 co iles tur
    public float tokenArmyCapPercentWhenReturn = 0.3f;

    [Header("Baza")]
    public GameObject basePrefab;  
    public int baseArmyCap = 700;                 // limit armii na polu bazy
    public int baseStartingArmy = 700;

    [Header("Walka")]
    [Range(0f, 2f)] public float winLossMin = 0.8f;
    [Range(0f, 2f)] public float winLossMax = 1.2f;

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;

    [Header("AI")]
    [Tooltip("Traktowane jako 'zasi�g ruchu' do wyszukiwania celu (promie� w heksach). Token i tak robi 1 krok na tur�.")]
    public int visionRadius = 2;

    [Header("AI - przeciwnik")]
    [Tooltip("Ustaw w Inspectorze albo w BotTurnManager: botA.enemyBot=botB i odwrotnie.")]
    public BotController enemyBot;

    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();
    private readonly List<Vector3Int> tokenLastPositions = new();
    private readonly List<bool> tokenNeedsCapUpgrade = new();
    private readonly HashSet<Vector3Int> reservedDestinations = new();
    private int virtualReservedPopulation = 0;

    private Vector3Int spawnPos;
    private bool initialized;
    private int tokenArmyCap;

    // ------------------------------------------------------------
    // Populacja pasywna co X tur
    // ------------------------------------------------------------
    [Header("Populacja - pasywny przyrost")]
    public int populationIncomeIntervalTurns = 10;
    [Range(0f, 1f)] public float populationIncomePercent = 0.20f;

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
        
        while (!map.IsGenerated)
            yield return null;

        spawnPos = (spawnNumber == 2) ? map.spawnPosPlayer2 : map.spawnPosPlayer1;
        
        map.SetOwnerAndTile(spawnPos, botOwnerId, botTile);
        
        tokenArmyCap = populationToCreateNewUnit; 
        
        if (map.TryGetCell(spawnPos, out var baseCell))
            baseCell.army = baseStartingArmy;

        SpawnBase();
        
        int tokenIndex = SpawnToken(spawnPos, initialArmySize: populationToCreateNewUnit);
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

    public int[] PriorityCounters { get; private set; } = new int[10];

    public string GetPriorityName(int index)
    {
        switch (index)
        {
            case 1: return "1) Kopalnia w zasiegu";
            case 2: return "2) Neutralne pole o najwiekszej populacji w zasiegu";
            case 3: return "3) Neutralne pole graniczne";
            case 4: return "4) Baza przeciwnika w zasiegu";
            case 5: return "5) Oddzial przeciwnika w zasiegu";
            case 6: return "6) Wrogie pole o najwiekszej populacji w zasiegu";
            case 7: return "7) Wrogie pole graniczne";
            case 8: return "8) Odwrot do bazy (<30% wojska)"; 
            case 9: return "9) Obrona terytorium (Przechwycenie wroga)";
            default: return "0) Ruch losowy (Fallback)";
        }
    }

    void DoTurn()
    {
        turnCounter++;

        if (populationIncomeIntervalTurns > 0 && (turnCounter % populationIncomeIntervalTurns) == 0)
        {
            AddPopulationFromOwnedTiles();
            IncreaseTokenArmyCap();
            for (int i = 0; i < tokenNeedsCapUpgrade.Count; i++)
            {
                tokenNeedsCapUpgrade[i] = true;
            }
        }
        reservedDestinations.Clear();
        virtualReservedPopulation = 0;

        //GainGoldForTurn();
        TryCreateNewUnitFromPopulation();
        
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            if (i < tokens.Count)
            {
                DoUnitStep(i);
            }
        }
    }

    void AddPopulationFromOwnedTiles()
    {
        if (map == null) return;

        int sum = 0;

        // map.DebugCells to Twoje "�r�d�o prawdy" - uwzgl�dnia te� utracone pola
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

        Debug.LogWarning($"Bot[{botOwnerId}] tokenArmyCap zwi�kszony do {tokenArmyCap}");
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
        
        if (currentPos == spawnPos)
        {
            RefillTokenUpToCapFromPopulation(unitIndex);
            tokenNeedsCapUpgrade[unitIndex] = false;
        }

        // NOWA LOGIKA: priorytety 1�7 wybieraj� CEL, a my robimy 1 krok w jego stron�
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
    // PRIORYTETY 1�7
    // ============================================================
bool TryChooseStepByPriorities(int unitIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        int attackerArmy = tokens[unitIndex].armySize;
        List<Vector3Int> inRange = map.GetNeighbours(currentPos);
        
        // NAJWYŻSZY PRIORYTET TAKTYCZNY: Zajęcie bazy wroga (jeśli widoczna i do wygrania)
        if (TryPickEnemyBaseInRange(currentPos, attackerArmy, out var t4))
        {
            PriorityCounters[4]++;
            reservedDestinations.Add(t4); 
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t4, out step);
        }

        // PRIORYTET 8: Odwrót krytyczny (<50% max limitu wojska)
        if (attackerArmy < tokenArmyCap * tokenArmyCapPercentWhenReturn)
        {
            int need = tokenArmyCap - attackerArmy;
            if ((population - virtualReservedPopulation) >= need)
            {
                virtualReservedPopulation += need;
                PriorityCounters[8]++; 
                return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, spawnPos, out step);
            }
        }

        // PRIORYTET 9: Obrona terytorium / Intercepcja atakującego tokenu wroga
        if (TryPickInterceptTarget(unitIndex, currentPos, out var t9))
        {
            PriorityCounters[9]++;
            reservedDestinations.Add(t9); // Rezerwacja pozycji wroga (zgodnie z systemem pól)
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t9, out step);
        }

        /*if (TryPickMineTarget(inRange, currentPos, attackerArmy, out var t1)) ... wyłączone */

        if (TryPickNeutralMaxPopInRange(inRange, currentPos, out var t2))
        {
            PriorityCounters[2]++;
            reservedDestinations.Add(t2);
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t2, out step);
        }

        if (TryPickNeutralBorderMaxPop(currentPos, out var t3))
        {
            PriorityCounters[3]++;
            reservedDestinations.Add(t3);
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t3, out step);
        }

        if (TryPickEnemyTokenInRange(currentPos, attackerArmy, out var t5))
        {
            PriorityCounters[5]++;
            reservedDestinations.Add(t5);
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t5, out step);
        }

        if (TryPickEnemyMaxPopInRangeAttackable(inRange, currentPos, attackerArmy, out var t6))
        {
            PriorityCounters[6]++;
            reservedDestinations.Add(t6);
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t6, out step);
        }

        if (TryPickEnemyBorderMaxPop(currentPos, out var t7))
        {
            PriorityCounters[7]++;
            reservedDestinations.Add(t7);
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t7, out step);
        }

        PriorityCounters[0]++; 
        return false;
    }
    

    // ---------- Priorytet 1 ----------
    bool TryPickMineTarget(List<Vector3Int> inRange, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        bool found = false;

        int bestDist = int.MaxValue;
        int bestPop = int.MinValue;

        foreach (var p in inRange)
        {
            //Debug.Log($"dist Hex={HexDist(currentPos, p)} bfs={TileDistBFS(currentPos, p, 5)} vision={visionRadius} p={p}");
            if (TileDistBFS(currentPos, p, visionRadius) > visionRadius) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;
            if (!cell.hasMine) continue;

            // interesuj� nas kopalnie nie-nasze (neutral lub wroga)
            if (cell.ownerId == botOwnerId) continue;

            // je�li to pole wroga, musi by� "do wygrania" (�eby AI nie robi�o samob�jstw)
            if (cell.ownerId != 0)
            {
                int def = Mathf.Max(0, cell.army);
                if (!(def <= 0 || attackerArmy > def))
                    continue;
            }

            int dist = HexDist(currentPos, p);
            int pop = cell.populationNumber;

            // preferuj bli�sze, a przy remisie wi�ksza populacja
            if (!found || dist < bestDist || (dist == bestDist && pop > bestPop))
            {
                found = true;
                bestDist = dist;
                bestPop = pop;
                target = p;
            }
        }

        return found;
    }

    // ---------- Priorytet 2 ----------
    bool TryPickNeutralMaxPopInRange(List<Vector3Int> inRange, Vector3Int currentPos, out Vector3Int target)
    {
        target = default;
        bool found = false;

        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in inRange)
        {
            if (reservedDestinations.Contains(p)) continue;
            if (TileDistBFS(currentPos, p, visionRadius) > visionRadius) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;
            if (cell.ownerId != 0) continue; // tylko neutral

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            // g��wnie max populacja, a przy remisie bli�ej
            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true;
                bestPop = pop;
                bestDist = dist;
                target = p;
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
            if (reservedDestinations.Contains(p)) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;
            if (cell.ownerId != 0) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true;
                bestPop = pop;
                bestDist = dist;
                target = p;
            }
        }

        return found;
    }

    // ---------- Priorytet 4 ----------
    bool TryPickEnemyBaseInRange(Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        if (enemyBot == null) return false;

        Vector3Int enemyBase = enemyBot.SpawnPos;
        if (reservedDestinations.Contains(enemyBase)) return false;
        if (HexDist(currentPos, enemyBase) > visionRadius) return false;


        if (!map.TryGetCell(enemyBase, out var cell)) return false;
        if (!cell.passable) return false;
        if (cell.ownerId == botOwnerId) return false; // ju� nasze

        int defender = Mathf.Max(0, cell.army);
        if (attackerArmy <= defender) return false;

        target = enemyBase;
        return true;
    }

    // ---------- Priorytet 5 ----------
    bool TryPickEnemyTokenInRange(Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        if (enemyBot == null) return false;

        bool found = false;
        int bestDist = int.MaxValue;
        int bestEnemyArmy = int.MinValue;

        for (int i = 0; i < enemyBot.TokenCount; i++)
        {
            Vector3Int pos = enemyBot.GetTokenPos(i);
            if (reservedDestinations.Contains(pos)) continue;
            int dist = HexDist(currentPos, pos);
            if (dist > visionRadius) continue;

            ArmyToken tok = enemyBot.GetToken(i);
            if (tok == null) continue;
            
            if (attackerArmy < tok.armySize) continue;

            // preferuj bli�sze; przy remisie atakuj najwi�kszy token kt�ry i tak wygrasz (�eby nie marnowa� tury)
            if (!found || dist < bestDist || (dist == bestDist && tok.armySize > bestEnemyArmy))
            {
                found = true;
                bestDist = dist;
                bestEnemyArmy = tok.armySize;
                target = pos;
            }
        }

        return found;
    }

    // ---------- Priorytet 6 ----------
    bool TryPickEnemyMaxPopInRangeAttackable(List<Vector3Int> inRange, Vector3Int currentPos, int attackerArmy, out Vector3Int target)
    {
        target = default;
        bool found = false;

        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in inRange)
        {
            if (reservedDestinations.Contains(p)) continue;
            if (TileDistBFS(currentPos, p, visionRadius) > visionRadius) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;

            if (cell.ownerId == 0) continue;              // nie neutral
            if (cell.ownerId == botOwnerId) continue;     // nie nasze

            // nie samob�jcze: musi by� do wygrania albo puste
            int def = Mathf.Max(0, cell.army);
            if (!(def <= 0 || attackerArmy > def))
                continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            // max populacja, a przy remisie bli�ej
            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true;
                bestPop = pop;
                bestDist = dist;
                target = p;
            }
        }

        return found;
    }

    // ---------- Priorytet 7 ----------
    bool TryPickEnemyBorderMaxPop(Vector3Int currentPos, out Vector3Int target)
    {
        target = default;

        var borderEnemies = GetBorderCells(ownerIdFilter: -1); // -1 = enemy (nie 0 i nie botOwnerId)
        if (borderEnemies.Count == 0) return false;

        bool found = false;
        int bestPop = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in borderEnemies)
        {
            if (reservedDestinations.Contains(p)) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;
            if (cell.ownerId == 0 || cell.ownerId == botOwnerId) continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            if (!found || pop > bestPop || (pop == bestPop && dist < bestDist))
            {
                found = true;
                bestPop = pop;
                bestDist = dist;
                target = p;
            }
        }

        return found;
    }

    // ------------------------------------------------------------
    // KROK w stron� targetu + unikanie cofki + unikanie wej�cia na nie-do-wygrania enemy tile
    // ------------------------------------------------------------
    bool TryStepTowardsTarget(Vector3Int currentPos, Vector3Int lastPos, int attackerArmy, Vector3Int target, out Vector3Int step)
    {
        step = default;

        if (target == currentPos) return false;
        if (!map.TryGetNextStep(currentPos, target, out var nextStep))
            return false;

        // je�li wybrany krok to cofka, spr�buj alternatywy bli�ej celu
        if (nextStep == lastPos)
            nextStep = PickBestNeighbourTowardsTarget(currentPos, lastPos, target);

        // je�li krok prowadzi na pole wroga nie-do-wygrania, spr�buj alternatywy
        if (!IsStepSafeForTileBattle(nextStep, attackerArmy))
        {
            var alt = PickBestNeighbourTowardsTarget(currentPos, lastPos, target, requireSafeEnemyStep: true, attackerArmy: attackerArmy);
            if (alt != nextStep)
                nextStep = alt;
        }

        // ostatnia walidacja
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
            int totalDefense = Mathf.Max(0, cell.army);
            
            if (enemyBot != null)
            {
                int enemyTokenIdx = enemyBot.FindTokenIndexAt(pos);
                if (enemyTokenIdx != -1)
                {
                    totalDefense += enemyBot.GetToken(enemyTokenIdx).armySize;
                }
            }

            return (totalDefense <= 0 || attackerArmy > totalDefense);
        }
        return true;
    }

    Vector3Int PickBestNeighbourTowardsTarget(
        Vector3Int currentPos,
        Vector3Int lastPos,
        Vector3Int target,
        bool requireSafeEnemyStep = false,
        int attackerArmy = 0)
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
    // Border cells: pola granicz�ce z moim terytorium
    // ownerIdFilter:
    // 0  -> neutralne
    // -1 -> wrogie (owner != 0 i != botOwnerId)
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

                if (ownerIdFilter == 0)
                {
                    if (owner == 0) result.Add(n);
                }
                else if (ownerIdFilter == -1)
                {
                    if (owner != 0 && owner != botOwnerId) result.Add(n);
                }
            }
        }

        return new List<Vector3Int>(result);
    }

    // ------------------------------------------------------------
    // Rekrutacja
    // ------------------------------------------------------------
    Vector3Int GetSpawnCellForNewToken()
    {
        // 1) Baza je�li wolna
        if (FindTokenIndexAt(spawnPos) == -1)
            return spawnPos;

        // 2) Je�li baza zaj�ta, szukamy wolnego s�siada (pierwszy wolny)
        var neighbours = map.GetNeighbours(spawnPos);
        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (FindTokenIndexAt(n) != -1) continue;
            return n;
        }

        // 3) Awaryjnie: baza (stack)
        return spawnPos;
    }

    void TryCreateNewUnitFromPopulation()
    {
        if (map.GetOwnerId(spawnPos) != botOwnerId) return;
        
        while (tokens.Count < maxArmyTokens && population >= populationToCreateNewUnit)
        {
            population -= populationToCreateNewUnit;

            Vector3Int spawnCell = GetSpawnCellForNewToken();
            
            int idx = SpawnToken(spawnCell, initialArmySize: populationToCreateNewUnit); 
            if (idx >= 0)
            {
                tokenPositions[idx] = spawnCell;
                tokenLastPositions[idx] = spawnCell;
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
            Debug.LogWarning("BotController: brak armyTokenPrefab lub armySprite - pionek nie b�dzie widoczny.");
            return -1;
        }

        ArmyToken token = Instantiate(armyTokenPrefab, transform);
        token.Init(armySprite);
        token.armySize = initialArmySize;
        token.TeleportToCell(map.tilemap, cell);

        tokens.Add(token);
        tokenPositions.Add(cell);
        tokenLastPositions.Add(cell);
        tokenNeedsCapUpgrade.Add(false);

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
    // Token vs Token � zostawiasz jak masz (Twoja wersja)
    // ------------------------------------------------------------
    // Zwraca liczbe stoczonych bitew polowych (token vs token)
    public int ResolveCollisionsWith(BotController other)
    {
        int battles = 0;
        if (other == null) return battles;

        for (int i = TokenCount - 1; i >= 0; i--)
        {
            if (i >= TokenCount) continue;

            Vector3Int pos = GetTokenPos(i);

            int j = other.FindTokenIndexAt(pos);
            if (j < 0) continue;

            ArmyToken aTok = GetToken(i);
            ArmyToken bTok = other.GetToken(j);
            battles++;

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

            /*if (hasMine && previousOwner != 0 && previousOwner != winner.botOwnerId)
            {
                loser.ownedMineCount = Mathf.Max(0, loser.ownedMineCount - 1);
            }*/
        }

        return battles;
    }

    // przej�cie pola po walce token vs token
    public void ClaimTileAfterTokenBattle(Vector3Int pos)
    {
        if (!map.TryGetCell(pos, out var cell)) return;

        int previousOwner = cell.ownerId;

        map.SetOwnerAndTile(pos, botOwnerId, botTile);
        cell.army = populationPerCapture;

        /*if (cell.hasMine && previousOwner != botOwnerId)
            ownedMineCount++;*/
    }

    // ------------------------------------------------------------
    // WEJ�CIE NA POLE
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

        /*if (cell.hasMine && previousOwner != botOwnerId)
            ownedMineCount++;*/
    }

    void KillToken(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return;

        Destroy(tokens[unitIndex].gameObject);

        tokens.RemoveAt(unitIndex);
        tokenPositions.RemoveAt(unitIndex);
        tokenLastPositions.RemoveAt(unitIndex);
        tokenNeedsCapUpgrade.RemoveAt(unitIndex);
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

        /*if (cell.hasMine)
            ownedMineCount++;*/
    }

    // ------------------------------------------------------------
    // Gold
    // ------------------------------------------------------------
    /*void GainGoldForTurn()
    {
        int income = goldPerIntervalByBase + (ownedMineCount * goldGainedByMine);
        gold += income;
    }*/

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

    int TileDistBFS(Vector3Int start, Vector3Int goal, int maxDepth)
    {
        if (start == goal) return 0;

        var q = new Queue<Vector3Int>();
        var dist = new Dictionary<Vector3Int, int>();

        q.Enqueue(start);
        dist[start] = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int d = dist[cur];
            if (d >= maxDepth) continue;

            foreach (var n in map.GetNeighbours(cur))
            {
                if (!map.IsPassableLand(n)) continue;
                if (dist.ContainsKey(n)) continue;

                int nd = d + 1;
                if (n == goal) return nd;

                dist[n] = nd;
                q.Enqueue(n);
            }
        }

        return int.MaxValue; // poza zasi�giem
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
    
    // ============================================================
    // LOGIKA SYSTEMU OBRONY TERYTORIUM (Z RESTRYKTEM WZROKU)
    // ============================================================
    bool TryPickInterceptTarget(int unitIndex, Vector3Int currentPos, out Vector3Int target)
    {
        target = default;
        if (enemyBot == null) return false;

        // Przeszukujemy tokeny wroga
        for (int j = 0; j < enemyBot.TokenCount; j++)
        {
            Vector3Int enemyPos = enemyBot.GetTokenPos(j);

            // Rezerwacja: Jeśli inny nasz token już poluje na tego wroga w tej turze, pomiń go
            if (reservedDestinations.Contains(enemyPos)) continue;

            // WZROK: Sprawdzamy, czy wrogi token jest w zasięgu wzroku terytorium lub naszych jednostek
            if (!IsEnemyVisibleToOurNetwork(enemyPos)) continue;

            // Wyznaczamy, który z naszych tokenów jest najlepszym kandydatem do obrony przed wrogiem 'j'
            int bestOurTokenIdx = FindBestDefenderForEnemyToken(j);

            // Jeśli to TEN aktualnie przetwarzany token jest wyznaczonym obrońcą – ruszaj do ataku
            if (bestOurTokenIdx == unitIndex)
            {
                target = enemyPos;
                return true;
            }
        }
        return false;
    }

    // Sprawdza, czy wróg znajduje się w polu widzenia terytorium lub jakiegokolwiek naszego tokenu
    bool IsEnemyVisibleToOurNetwork(Vector3Int enemyPos)
    {
        // 1. Czy wróg stoi na naszym terytorium LUB w promieniu visionRadius od jakiegokolwiek naszego kafelka?
        // (Jeśli stoi na naszym polu, dist wynosi 0, czyli warunek 0 <= visionRadius jest automatycznie spełniony)
        foreach (var cell in map.DebugCells)
        {
            if (cell.ownerId == botOwnerId && HexDist(cell.coord, enemyPos) <= visionRadius)
                return true;
        }

        // 2. Czy wróg znajduje się w zasięgu wzroku (visionRadius) któregokolwiek z naszych żywych tokenów?
        for (int i = 0; i < tokens.Count; i++)
        {
            if (HexDist(tokenPositions[i], enemyPos) <= visionRadius)
                return true;
        }

        // Jeśli wyszedł poza nasz zasięg wzroku i terytorium – zgubiliśmy go w mgle wojny
        return false;
    }

    int FindBestDefenderForEnemyToken(int enemyTokenIdx)
    {
        Vector3Int enemyPos = enemyBot.GetTokenPos(enemyTokenIdx);
        ArmyToken enemyToken = enemyBot.GetToken(enemyTokenIdx);
        if (enemyToken == null) return -1;

        int enemyArmy = enemyToken.armySize;
        int bestTokenIdx = -1;
        int minDistance = int.MaxValue;

        // STRATEGIA A: Szukamy najbliższego naszego tokenu, który ma WIĘCEJ wojska niż wróg
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].armySize > enemyArmy)
            {
                int dist = HexDist(tokenPositions[i], enemyPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestTokenIdx = i;
                }
            }
        }

        // STRATEGIA B: Jeśli nie posiadamy silniejszego tokenu, wysyłamy po prostu ten najbliższy
        if (bestTokenIdx == -1)
        {
            minDistance = int.MaxValue;
            for (int i = 0; i < tokens.Count; i++)
            {
                int dist = HexDist(tokenPositions[i], enemyPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestTokenIdx = i;
                }
            }
        }

        return bestTokenIdx;
    }
    

    public void ResetBotState()
    {
        // 1. Fizycznie niszczymy obiekty tokenów ze sceny i czyścimy listy
        foreach (var t in tokens)
        {
            if (t != null) Destroy(t.gameObject);
        }
        tokens.Clear();
        tokenPositions.Clear();
        tokenLastPositions.Clear();
        tokenNeedsCapUpgrade.Clear();
        reservedDestinations.Clear();

        // 2. Niszczymy obiekt starej bazy
        if (spawnedBase != null) Destroy(spawnedBase);
        spawnedBase = null;

        // 3. Zerujemy liczniki i rezerwacje pamięci
        System.Array.Clear(PriorityCounters, 0, PriorityCounters.Length);
        virtualReservedPopulation = 0;
        turnCounter = 0;
        population = 0; // Reset zasobów do stanu zero przed nową rekrutacją startową
        initialized = false;

        // 4. Odpalamy ponownie procedurę ustawienia bazy i tokenu startowego
        spawnPos = (spawnNumber == 2) ? map.spawnPosPlayer2 : map.spawnPosPlayer1;
        map.SetOwnerAndTile(spawnPos, botOwnerId, botTile);
        tokenArmyCap = populationToCreateNewUnit; 
        
        if (map.TryGetCell(spawnPos, out var baseCell))
            baseCell.army = baseStartingArmy;

        SpawnBase();
        
        int tokenIndex = SpawnToken(spawnPos, initialArmySize: populationToCreateNewUnit);
        if (tokenIndex >= 0)
        {
            tokenPositions[tokenIndex] = spawnPos;
            tokenLastPositions[tokenIndex] = spawnPos;
        }

        initialized = true;
    }


}
