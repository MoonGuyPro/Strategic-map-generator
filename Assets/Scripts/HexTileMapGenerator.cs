using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMapGenerator : MonoBehaviour
{
    // Wewnêtrzna klasa danych - Unity siê do niej nie czepia
    [System.Serializable]
    private class HexCell
    {
        public Vector3Int coord;   // wspó³rzêdne w Tilemapie
        public bool isWater;
        public bool passable;
        public int ownerId;        // 0 = neutral, 1 = player1, 2 = player2...
        public bool hasMine;
        public bool isSpawn;
    }

    [Header("Tilemap i Tiles")]
    public Tilemap tilemap;
    public TileBase grassTile;
    public TileBase waterTile;
    public TileBase spawnTile;

    [Header("Rozmiar mapy")]
    public int width = 20;
    public int height = 20;

    [Range(0f, 1f)]
    public float waterProbability = 0.2f;

    [Header("Spawny graczy")]
    public int minSpawnDistance = 10;

    // Stan gry
    Dictionary<Vector3Int, HexCell> cells = new Dictionary<Vector3Int, HexCell>();

    // Dla podgl¹du w Inspectorze
    public Vector3Int spawnPosPlayer1;
    public Vector3Int spawnPosPlayer2;

    void Start()
    {
        if (tilemap == null)
        {
            Debug.LogError("HexMapGenerator: tilemap nie jest przypisana!");
            return;
        }
        if (grassTile == null || waterTile == null || spawnTile == null)
        {
            Debug.LogError("HexMapGenerator: nie wszystkie TileBase s¹ przypisane!");
            return;
        }

        GenerateMap();
        GeneratePlayerSpawns();
    }

    // ------------------------------------------------------------
    // GENEROWANIE MAPY + HexCell
    // ------------------------------------------------------------
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

                var cell = new HexCell
                {
                    coord = pos,
                    isWater = isWater,
                    passable = !isWater,
                    ownerId = 0,
                    hasMine = false,
                    isSpawn = false
                };

                cells[pos] = cell;
            }
        }
    }

    // ------------------------------------------------------------
    // GENEROWANIE SPAWNÓW
    // ------------------------------------------------------------
    void GeneratePlayerSpawns()
    {
        // Kandydaci na spawny – tylko l¹d i przechodnie pola
        List<HexCell> candidates = new List<HexCell>();
        foreach (var kvp in cells)
        {
            HexCell cell = kvp.Value;
            if (cell.passable && !cell.isWater)
                candidates.Add(cell);
        }

        if (candidates.Count < 2)
        {
            Debug.LogError("Za ma³o l¹du, ¿eby wygenerowaæ 2 spawny.");
            return;
        }

        // 1. Losujemy pierwszy spawn
        HexCell spawn1 = candidates[Random.Range(0, candidates.Count)];

        // 2. Szukamy pól w odpowiedniej odleg³oœci + najdalszego
        List<HexCell> farEnough = new List<HexCell>();
        int maxDist = 0;
        HexCell farthest = null;

        foreach (HexCell cell in candidates)
        {
            int d = HexDistanceOddR(spawn1.coord, cell.coord);

            if (d > maxDist)
            {
                maxDist = d;
                farthest = cell;
            }

            if (d >= minSpawnDistance)
                farEnough.Add(cell);
        }

        HexCell spawn2;
        if (farEnough.Count > 0)
        {
            spawn2 = farEnough[Random.Range(0, farEnough.Count)];
        }
        else
        {
            Debug.LogWarning($"Brak pola w odleg³oœci >= {minSpawnDistance}. U¿ywam najdalszego (dist={maxDist}).");
            spawn2 = farthest;
        }

        // Ustawiamy stan
        spawn1.isSpawn = true;
        spawn2.isSpawn = true;
        spawn1.ownerId = 1;  // gracz 1
        spawn2.ownerId = 2;  // gracz 2

        spawnPosPlayer1 = spawn1.coord;
        spawnPosPlayer2 = spawn2.coord;

        // Podmieniamy tile na spawnTile
        tilemap.SetTile(spawn1.coord, spawnTile);
        tilemap.SetTile(spawn2.coord, spawnTile);

        Debug.Log($"Spawn1: {spawn1.coord}, Spawn2: {spawn2.coord}, dist = {HexDistanceOddR(spawn1.coord, spawn2.coord)}");
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

    // Odd-R pointy-top -> cube coords
    Vector3Int OddRToCube(Vector3Int h)
    {
        int x = h.x - (h.y - (h.y & 1)) / 2;
        int z = h.y;
        int y = -x - z;
        return new Vector3Int(x, y, z);
    }

    // ------------------------------------------------------------
    // PRZYK£AD: pobranie danych pola po klikniêciu
    // ------------------------------------------------------------
    public object GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        if (cells.TryGetValue(cellPos, out HexCell cell))
            return cell;
        return null;
    }
}
