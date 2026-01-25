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

    [Header("Walka")]
    [Range(0f, 2f)] public float winLossMin = 0.8f;
    [Range(0f, 2f)] public float winLossMax = 1.2f;

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;

    [Header("AI")]
    [Tooltip("Traktowane jako 'zasiêg ruchu' do wyszukiwania celu (promieñ w heksach). Token i tak robi 1 krok na turê.")]
    public int visionRadius = 1;

    [Header("AI - przeciwnik")]
    [Tooltip("Ustaw w Inspectorze albo w BotTurnManager: botA.enemyBot=botB i odwrotnie.")]
    public BotController enemyBot;

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

        yield return null;

        spawnPos = (spawnNumber == 2) ? map.spawnPosPlayer2 : map.spawnPosPlayer1;

        map.SetOwnerAndTile(spawnPos, botOwnerId, botTile);

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

        for (int i = tokens.Count - 1; i >= 0; i--)
            DoUnitStep(i);
    }

    void DoUnitStep(int unitIndex)
    {
        if (unitIndex < 0 || unitIndex >= tokens.Count) return;

        Vector3Int currentPos = tokenPositions[unitIndex];
        Vector3Int lastPos = tokenLastPositions[unitIndex];

        // NOWA LOGIKA: priorytety 1–7 wybieraj¹ CEL, a my robimy 1 krok w jego stronê
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
    // PRIORYTETY 1–7
    // ============================================================
    bool TryChooseStepByPriorities(int unitIndex, Vector3Int currentPos, Vector3Int lastPos, out Vector3Int step)
    {
        step = default;
        if (unitIndex < 0 || unitIndex >= tokens.Count) return false;

        int attackerArmy = tokens[unitIndex].armySize;

        // Zasiêg "ruchu" (wyszukiwania celu)
        List<Vector3Int> inRange = map.GetNeighbours(currentPos);

        // 1) Kopalnie w zasiêgu -> idŸ na nie (preferuj bli¿sze)
        if (TryPickMineTarget(inRange, currentPos, attackerArmy, out var t1))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Kopalnia");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t1, out step);
        }


        // 2) Neutral z najwiêksz¹ populacj¹ w zasiêgu -> idŸ na niego
        if (TryPickNeutralMaxPopInRange(inRange, currentPos, out var t2))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Pole neutralne w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t2, out step);
        }


        // 3) Neutral border (graniczy z moim terytorium) z najwiêksz¹ populacj¹ -> idŸ w jego kierunku
        if (TryPickNeutralBorderMaxPop(currentPos, out var t3))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Neutral border");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t3, out step);
        }


        // 4) Baza przeciwnika w zasiêgu -> zaatakuj, jeœli masz wiêksz¹ armiê ni¿ pole
        if (TryPickEnemyBaseInRange(currentPos, attackerArmy, out var t4))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Baza przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t4, out step);
        }


        // 5) Token przeciwnika w zasiêgu -> zaatakuj, jeœli masz wiêksz¹ armiê
        if (TryPickEnemyTokenInRange(currentPos, attackerArmy, out var t5))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Token przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t5, out step);
        }


        // 6) Pole przeciwnika z najwiêksz¹ populacj¹ w zasiêgu -> zaatakuj (tu bez warunku "mam wiêksz¹", bo punkt nie mówi,
        // ale ¯EBY NIE ROBIÆ SAMOBÓJSTW filtrujê tylko atakowalne)
        if (TryPickEnemyMaxPopInRangeAttackable(inRange, currentPos, attackerArmy, out var t6))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Atak na pole przeciwnika w zasiêgu");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t6, out step);
        }


        // 7) Pole przeciwnika border z najwiêksz¹ populacj¹ -> idŸ w jego kierunku
        if (TryPickEnemyBorderMaxPop(currentPos, out var t7))
        {
            Debug.Log($"Bot: '{botOwnerId}' Cel: Atak na pole przeciwnika border z najwiêksz¹ populacj¹");
            return TryStepTowardsTarget(currentPos, lastPos, attackerArmy, t7, out step);
        }


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

            // interesuj¹ nas kopalnie nie-nasze (neutral lub wroga)
            if (cell.ownerId == botOwnerId) continue;

            // jeœli to pole wroga, musi byæ "do wygrania" (¿eby AI nie robi³o samobójstw)
            if (cell.ownerId != 0)
            {
                int def = Mathf.Max(0, cell.army);
                if (!(def <= 0 || attackerArmy > def))
                    continue;
            }

            int dist = HexDist(currentPos, p);
            int pop = cell.populationNumber;

            // preferuj bli¿sze, a przy remisie wiêksza populacja
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
            if (TileDistBFS(currentPos, p, visionRadius) > visionRadius) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;
            if (cell.ownerId != 0) continue; // tylko neutral

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            // g³ównie max populacja, a przy remisie bli¿ej
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
        if (HexDist(currentPos, enemyBase) > visionRadius) return false;


        if (!map.TryGetCell(enemyBase, out var cell)) return false;
        if (!cell.passable) return false;
        if (cell.ownerId == botOwnerId) return false; // ju¿ nasze

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
            int dist = HexDist(currentPos, pos);
            if (dist > visionRadius) continue;

            ArmyToken tok = enemyBot.GetToken(i);
            if (tok == null) continue;

            // atak tylko jeœli jesteœmy wiêksi
            if (attackerArmy <= tok.armySize) continue;

            // preferuj bli¿sze; przy remisie atakuj najwiêkszy token który i tak wygrasz (¿eby nie marnowaæ tury)
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
            if (TileDistBFS(currentPos, p, visionRadius) > visionRadius) continue;
            if (!map.TryGetCell(p, out var cell)) continue;
            if (!cell.passable) continue;

            if (cell.ownerId == 0) continue;              // nie neutral
            if (cell.ownerId == botOwnerId) continue;     // nie nasze

            // nie samobójcze: musi byæ do wygrania albo puste
            int def = Mathf.Max(0, cell.army);
            if (!(def <= 0 || attackerArmy > def))
                continue;

            int pop = cell.populationNumber;
            int dist = HexDist(currentPos, p);

            // max populacja, a przy remisie bli¿ej
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
    // KROK w stronê targetu + unikanie cofki + unikanie wejœcia na nie-do-wygrania enemy tile
    // ------------------------------------------------------------
    bool TryStepTowardsTarget(Vector3Int currentPos, Vector3Int lastPos, int attackerArmy, Vector3Int target, out Vector3Int step)
    {
        step = default;

        if (target == currentPos) return false;
        if (!map.TryGetNextStep(currentPos, target, out var nextStep))
            return false;

        // jeœli wybrany krok to cofka, spróbuj alternatywy bli¿ej celu
        if (nextStep == lastPos)
            nextStep = PickBestNeighbourTowardsTarget(currentPos, lastPos, target);

        // jeœli krok prowadzi na pole wroga nie-do-wygrania, spróbuj alternatywy
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

        // jeœli to wrogie pole, upewnij siê ¿e da siê je wygraæ (¿eby AI nie traci³o tokenów bez sensu)
        if (cell.ownerId != 0 && cell.ownerId != botOwnerId)
        {
            int def = Mathf.Max(0, cell.army);
            return (def <= 0 || attackerArmy > def);
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
    // Border cells: pola granicz¹ce z moim terytorium
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
            baseCell.army += baseArmyBonus;
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
    // Token vs Token – zostawiasz jak masz (Twoja wersja)
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

    // przejêcie pola po walce token vs token
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

        return int.MaxValue; // poza zasiêgiem
    }

}
