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
    public int populationPerCapture = 10; // ile zabieramy z pola

    [Header("Z³oto")]
    public int gold = 0;
    public int goldPerIntervalByBase = 70;
    public int goldGainedByMine = 30;
    public int ownedMineCount = 0;

    [Header("Armia")]
    public ArmyToken armyTokenPrefab;
    public Sprite armySprite;

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;
    public float expansionInterval = 5f;

    [Header("AI")]
    public int visionRadius = 2;

    // pod przysz³oœæ: wiele pionków
    private readonly List<ArmyToken> tokens = new();
    private readonly List<Vector3Int> tokenPositions = new();

    private Vector3Int currentPos;
    private Vector3Int lastPos;
    private float timer;
    private bool initialized;

    private Vector3Int SpawnPos =>
        spawnNumber == 2 ? map.spawnPosPlayer2 : map.spawnPosPlayer1;

    private System.Collections.IEnumerator Start()
    {
        if (map == null || botTile == null)
        {
            Debug.LogError("BotController: brak map lub botTile!");
            yield break;
        }

        yield return null; // czekamy a¿ mapa siê wygeneruje

        currentPos = SpawnPos;
        lastPos = currentPos;

        map.SetOwnerAndTile(currentPos, botOwnerId, botTile);

        SpawnToken(currentPos);

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
    // Tokeny
    // ------------------------------------------------------------
    void SpawnToken(Vector3Int cell)
    {
        if (armyTokenPrefab == null || armySprite == null)
        {
            Debug.LogWarning("BotController: brak armyTokenPrefab lub armySprite");
            return;
        }

        ArmyToken token = Instantiate(armyTokenPrefab, transform);
        token.Init(armySprite);
        token.TeleportToCell(map.tilemap, cell);

        tokens.Add(token);
        tokenPositions.Add(cell);
    }

    void UpdateToken(int index, Vector3Int cell)
    {
        if (index < 0 || index >= tokens.Count) return;

        tokens[index].TeleportToCell(map.tilemap, cell);
        tokenPositions[index] = cell;
    }


    // ------------------------------------------------------------
    // Logika tury (na razie token 0)
    // ------------------------------------------------------------
    void DoTurn()
    {
        GainGoldForTurn();

        // 1) wybieramy najlepszy krok o 1 pole (priorytet: przejêcie teraz)
        if (TryChooseBestCaptureStep(out var step))
        {
            // przejmujemy neutralne pole
            CaptureCell(step);

            lastPos = currentPos;
            currentPos = step;
            UpdateToken(0, currentPos);
            return;
        }

        // 2) jeœli nie ma neutralnych s¹siadów - dopiero wtedy ruch "po swoim"
        MoveFallback();
    }

    bool TryChooseBestCaptureStep(out Vector3Int bestStep)
    {
        bestStep = default;

        // kopalnie widziane w promieniu 2 (tylko przechodnie)
        List<Vector3Int> visible = map.GetCellsInRange(currentPos, visionRadius);

        List<Vector3Int> minesInSight = new();
        foreach (var v in visible)
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

            // interesuj¹ nas tylko NEUTRALNE kroki (bo mamy przejmowaæ co turê)
            if (map.GetOwnerId(n) != 0) continue;

            if (!map.TryGetCell(n, out var cell)) continue;

            int score = 0;

            // (1) jeœli ten krok to kopalnia -> giga priorytet
            if (cell.hasMine) score += 1_000_000;

            // (2) jeœli widzimy kopalniê w promieniu 2:
            //     premiuj krok, który skraca dystans do najbli¿szej kopalni
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

                // jeœli krok faktycznie przybli¿a do kopalni -> du¿y bonus
                if (bestMineDistAfter < bestMineDistNow)
                    score += 100_000;
            }

            // (3) populacja jako tie-breaker / drugi priorytet
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

    void CaptureCell(Vector3Int cellPos)
    {
        if (!map.TryGetCell(cellPos, out HexCell cell))
            return;

        // przejmujemy tylko neutralne
        if (cell.ownerId != 0)
            return;

        // 1. zmiana w³aœciciela + tile
        map.SetOwnerAndTile(cellPos, botOwnerId, botTile);

        // 2. bot zbiera populacjê (populacja - 10)
        int gainedPopulation = Mathf.Max(0, cell.populationNumber - populationPerCapture);
        population += gainedPopulation;

        // 3. pole dostaje garnizon
        cell.army = populationPerCapture;

        // 4. JEŒLI TO KOPALNIA -> ZWIÊKSZ LICZNIK
        if (cell.hasMine)
        {
            ownedMineCount++;
        }
    }



    void MoveFallback()
    {
        var neighbours = map.GetNeighbours(currentPos);

        List<Vector3Int> passable = new();

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            passable.Add(n);
        }

        if (passable.Count == 0) return;

        // chodzimy losowo po przechodnich (mo¿e byæ po swoim / po cudzym)
        var step = passable[Random.Range(0, passable.Count)];

        lastPos = currentPos;
        currentPos = step;
        UpdateToken(0, currentPos);
    }


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
    void GainGoldForTurn()
    {
        int income =
            goldPerIntervalByBase +
            (ownedMineCount * goldGainedByMine);

        gold += income;

        // opcjonalny log do sprawdzenia:
        // Debug.Log($"Bot {botOwnerId} income={income} (base={goldPerIntervalByBase}, mines={mines}*{goldGainedByMine}), gold={gold}");
    }


}
