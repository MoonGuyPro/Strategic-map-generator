using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMapGenerator : MonoBehaviour
{
    [Header("Tilemap i Tiles")]
    public Tilemap tilemap;
    public TileBase grassTile;
    public TileBase waterTile;
    public TileBase spawnTile;

    [Header("Populacja")]
    public int population_min = 11;
    public int population_max = 51;

    [Header("Kopalnie")]
    public TileBase mineTile;
    public int mineCount = 5;

    [Header("Rozmiar mapy")]
    public int width = 20;
    public int height = 20;

    [Range(0f, 1f)]
    public float waterProbability = 0.2f;

    [Header("Spawny graczy")]
    public int minSpawnDistance = 10;

    [Header("Debug")]
    [SerializeField] private List<HexCell> debugCells = new List<HexCell>();
    public IReadOnlyList<HexCell> DebugCells => debugCells;

    // Stan gry
    private Dictionary<Vector3Int, HexCell> cells = new Dictionary<Vector3Int, HexCell>();

    // Dla podgl¹du w Inspectorze
    public Vector3Int spawnPosPlayer1;
    public Vector3Int spawnPosPlayer2;

    public bool IsGenerated { get; private set; }

    void Start()
    {
        IsGenerated = false;

        if (tilemap == null)
        {
            Debug.LogError("HexMapGenerator: tilemap nie jest przypisana!");
            return;
        }
        if (grassTile == null || waterTile == null || spawnTile == null || mineTile == null)
        {
            Debug.LogError("HexMapGenerator: nie wszystkie TileBase s¹ przypisane (grass/water/spawn/mine)!");
            return;
        }

        GenerateMap();
        GeneratePlayerSpawns();
        GenerateMines();

        RefreshDebugList();

        IsGenerated = true;
    }

    void GenerateMap()
    {
        cells.Clear();
        tilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                bool isWater = Random.value < waterProbability;
                TileBase tileToPlace = isWater ? waterTile : grassTile;
                tilemap.SetTile(pos, tileToPlace);

                int population = isWater ? 0 : Random.Range(population_min, population_max); // 10ï¿½100

                HexCell cell = new HexCell
                {
                    coord = pos,
                    isWater = isWater,
                    passable = !isWater,
                    ownerId = 0,           // 0 = neutral
                    hasMine = false,
                    isSpawn = false,
                    populationNumber = population
                };

                cells[pos] = cell;
            }
        }
    }

    // ------------------------------------------------------------
    // GENEROWANIE SPAWNï¿½W
    // ------------------------------------------------------------
    void GeneratePlayerSpawns()
    {
        // Kandydaci na spawny – tylko l¹d i przechodnie pola + musi mieæ 6 przechodnich s¹siadów
        List<HexCell> candidates = new List<HexCell>();
        foreach (var kvp in cells)
        {
            HexCell cell = kvp.Value;
            if (!cell.passable || cell.isWater) continue;
            if (!IsGoodSpawnCell(cell.coord)) continue;

            candidates.Add(cell);
        }

        if (candidates.Count < 2)
        {
            Debug.LogError("Za ma³o poprawnych pól (z 6 przechodnimi s¹siadami), ¿eby wygenerowaæ 2 spawny.");
            return;
        }

        // 1) Losujemy spawn1 z dobrych kandydatów
        HexCell spawn1 = candidates[Random.Range(0, candidates.Count)];

        // 2) Szukamy spawn2: spe³nia minSpawnDistance, a jeœli siê nie da to bierzemy najdalszy
        List<HexCell> farEnough = new List<HexCell>();
        int maxDist = -1;
        HexCell farthest = null;

        foreach (HexCell cell in candidates)
        {
            if (cell == spawn1) continue;

            int d = HexDistanceOddR(spawn1.coord, cell.coord);

            if (d > maxDist)
            {
                maxDist = d;
                farthest = cell;
            }

            if (d >= minSpawnDistance)
                farEnough.Add(cell);
        }

        HexCell spawn2 = (farEnough.Count > 0)
            ? farEnough[Random.Range(0, farEnough.Count)]
            : farthest;

        // Ustawiamy stan
        spawn1.isSpawn = true;
        spawn2.isSpawn = true;
        spawn1.ownerId = 1;
        spawn2.ownerId = 2;

        spawnPosPlayer1 = spawn1.coord;
        spawnPosPlayer2 = spawn2.coord;

        // Podmieniamy tile na spawnTile
        tilemap.SetTile(spawn1.coord, spawnTile);
        tilemap.SetTile(spawn2.coord, spawnTile);

        Debug.Log($"Spawn1: {spawn1.coord}, Spawn2: {spawn2.coord}, dist = {HexDistanceOddR(spawn1.coord, spawn2.coord)}");
    }


    // ------------------------------------------------------------
    // GENEROWANIE KOPALNI
    // ------------------------------------------------------------
    void GenerateMines()
    {
        // pola zakazane dla kopalni: spawn + s¹siedzi spawnów
        HashSet<Vector3Int> forbidden = BuildForbiddenMineCells();

        // kandydaci: l¹d, przechodnie, nie-spawn, bez kopalni, nie w forbidden
        List<HexCell> candidates = new List<HexCell>();
        foreach (var kvp in cells)
        {
            HexCell cell = kvp.Value;

            if (!cell.passable || cell.isWater) continue;
            if (cell.isSpawn) continue;
            if (cell.hasMine) continue;
            if (forbidden.Contains(cell.coord)) continue;

            candidates.Add(cell);
        }

        if (candidates.Count == 0) return;

        // ¿eby by³o stabilniej: losowo tasujemy i idziemy po kolei
        Shuffle(candidates);

        int placed = 0;

        for (int i = 0; i < candidates.Count && placed < mineCount; i++)
        {
            HexCell cell = candidates[i];

            // warunek: ¿aden s¹siad nie ma kopalni
            if (!CanPlaceMineHere(cell.coord))
                continue;

            cell.hasMine = true;
            tilemap.SetTile(cell.coord, mineTile);
            placed++;
        }

        if (placed < mineCount)
            Debug.LogWarning($"Nie uda³o siê postawiæ wszystkich kopalni. Postawiono {placed}/{mineCount} (za ma³o miejsca przez ograniczenia).");
    }


    void RefreshDebugList()
    {
        debugCells.Clear();
        debugCells.AddRange(cells.Values);
    }

    // ------------------------------------------------------------
    // DYSTANS NA HEXACH (odd-r / point-top)
    // ------------------------------------------------------------
    int HexDistanceOddR(Vector3Int a, Vector3Int b)
    {
        Vector3Int ac = OddRToCube(a);
        Vector3Int bc = OddRToCube(b);

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

    // API
    public bool TryGetCell(Vector3Int coord, out HexCell cell) => cells.TryGetValue(coord, out cell);

    public bool IsPassableLand(Vector3Int coord)
    {
        if (cells.TryGetValue(coord, out HexCell cell))
            return cell.passable && !cell.isWater;
        return false;
    }

    public int GetOwnerId(Vector3Int coord)
    {
        if (cells.TryGetValue(coord, out HexCell cell))
            return cell.ownerId;
        return -1;
    }

    public void SetOwnerAndTile(Vector3Int coord, int newOwnerId, TileBase tile)
    {
        if (cells.TryGetValue(coord, out HexCell cell))
        {
            cell.ownerId = newOwnerId;
            tilemap.SetTile(coord, tile);
        }
    }

    public List<Vector3Int> GetNeighbours(Vector3Int coord)
    {
        List<Vector3Int> result = new List<Vector3Int>();

        bool isOdd = (coord.y & 1) == 1;

        Vector3Int[] evenOffsets =
        {
            new Vector3Int(+1,  0, 0),
            new Vector3Int( 0, +1, 0),
            new Vector3Int(-1, +1, 0),
            new Vector3Int(-1,  0, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int( 0, -1, 0),
        };

        Vector3Int[] oddOffsets =
        {
            new Vector3Int(+1,  0, 0),
            new Vector3Int(+1, +1, 0),
            new Vector3Int( 0, +1, 0),
            new Vector3Int(-1,  0, 0),
            new Vector3Int( 0, -1, 0),
            new Vector3Int(+1, -1, 0),
        };

        var offsets = isOdd ? oddOffsets : evenOffsets;

        foreach (var off in offsets)
        {
            Vector3Int n = coord + off;
            if (cells.ContainsKey(n))
                result.Add(n);
        }

        return result;
    }

    public bool TryGetNextStep(Vector3Int from, Vector3Int target, out Vector3Int nextStep)
    {
        nextStep = from;
        if (from == target) return false;

        var q = new Queue<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var visited = new HashSet<Vector3Int>();

        visited.Add(from);
        q.Enqueue(from);

        bool found = false;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (p == target) { found = true; break; }

            foreach (var n in GetNeighbours(p))
            {
                if (visited.Contains(n)) continue;
                if (!IsPassableLand(n)) continue;

                visited.Add(n);
                cameFrom[n] = p;
                q.Enqueue(n);
            }
        }

        if (!found) return false;

        var cur = target;
        while (cameFrom.TryGetValue(cur, out var prev) && prev != from)
            cur = prev;

        nextStep = cur;
        return true;
    }

    bool IsGoodSpawnCell(Vector3Int pos)
    {
        // musi byæ l¹dem i przechodnie (dla pewnoœci)
        if (!cells.TryGetValue(pos, out var c)) return false;
        if (!c.passable || c.isWater) return false;

        // wszystkie 6 s¹siadów musi istnieæ i byæ przechodnim l¹dem
        var neigh = GetNeighbours(pos);
        if (neigh.Count < 6) return false; // krawêdŸ mapy odpada

        foreach (var n in neigh)
        {
            if (!cells.TryGetValue(n, out var nc)) return false;
            if (!nc.passable || nc.isWater) return false;
        }

        return true;
    }

    HashSet<Vector3Int> BuildForbiddenMineCells()
    {
        var forbidden = new HashSet<Vector3Int>();

        // spawn + s¹siedzi spawnów
        void AddSpawnBlock(Vector3Int spawn)
        {
            forbidden.Add(spawn);
            foreach (var n in GetNeighbours(spawn))
                forbidden.Add(n);
        }

        AddSpawnBlock(spawnPosPlayer1);
        AddSpawnBlock(spawnPosPlayer2);

        return forbidden;
    }

    bool CanPlaceMineHere(Vector3Int pos)
    {
        // pole musi istnieæ i byæ sensowne
        if (!cells.TryGetValue(pos, out var c)) return false;
        if (!c.passable || c.isWater || c.isSpawn || c.hasMine) return false;

        // s¹siedzi kopalni nie mog¹ byæ kopalni¹
        foreach (var n in GetNeighbours(pos))
        {
            if (cells.TryGetValue(n, out var nc) && nc.hasMine)
                return false;
        }

        return true;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

}
