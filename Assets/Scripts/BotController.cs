using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BotController : MonoBehaviour
{
    [Header("Referencje")]
    public HexMapGenerator map;
    public TileBase botTile;

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
        tokens[index].TeleportToCell(map.tilemap, cell);
        tokenPositions[index] = cell;
    }

    // ------------------------------------------------------------
    // Logika tury (na razie token 0)
    // ------------------------------------------------------------
    void DoTurn()
    {
        List<Vector3Int> visible = map.GetCellsInRange(currentPos, visionRadius);
        Vector3Int? target = ChooseTarget(visible);

        if (!target.HasValue)
        {
            MoveFallback();
            return;
        }

        if (map.TryGetNextStep(currentPos, target.Value, out var step))
        {
            step = PreferNeutralStep(step, target.Value);

            if (map.GetOwnerId(step) == 0)
                map.SetOwnerAndTile(step, botOwnerId, botTile);

            lastPos = currentPos;
            currentPos = step;
            UpdateToken(0, currentPos);
        }
        else
        {
            MoveFallback();
        }
    }

    Vector3Int? ChooseTarget(List<Vector3Int> visible)
    {
        Vector3Int? bestMine = null;
        Vector3Int? bestPop = null;
        int bestPopulation = int.MinValue;

        foreach (var c in visible)
        {
            if (!map.TryGetCell(c, out var cell)) continue;
            if (!cell.passable) continue;

            if (cell.hasMine && cell.ownerId != botOwnerId)
                bestMine ??= c;

            if (cell.ownerId == 0 && cell.populationNumber > bestPopulation)
            {
                bestPopulation = cell.populationNumber;
                bestPop = c;
            }
        }

        return bestMine ?? bestPop;
    }

    Vector3Int PreferNeutralStep(Vector3Int suggested, Vector3Int target)
    {
        if (map.GetOwnerId(suggested) == 0)
            return suggested;

        int bestDist = HexDist(suggested, target);
        Vector3Int best = suggested;

        foreach (var n in map.GetNeighbours(currentPos))
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            if (map.GetOwnerId(n) == 0 && HexDist(n, target) <= bestDist)
                best = n;
        }

        return best;
    }

    void MoveFallback()
    {
        foreach (var n in map.GetNeighbours(currentPos))
        {
            if (!map.IsPassableLand(n)) continue;
            if (n == lastPos) continue;

            if (map.GetOwnerId(n) == 0)
            {
                map.SetOwnerAndTile(n, botOwnerId, botTile);
                lastPos = currentPos;
                currentPos = n;
                UpdateToken(0, currentPos);
                return;
            }
        }
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
}
