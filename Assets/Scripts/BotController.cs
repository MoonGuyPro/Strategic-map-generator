using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BotController : MonoBehaviour
{
    [Header("Referencje")]
    public HexMapGenerator map;    // przypisz w Inspectorze
    public TileBase botTile;       // tile oznaczaj¹cy terytorium bota

    [Header("Ustawienia bota")]
    public int botOwnerId = 1;     // 1 dla bota 1, 2 dla bota 2
    [Tooltip("1 = spawnPosPlayer1, 2 = spawnPosPlayer2")]
    public int spawnNumber = 1;    // który spawn ma byæ startem tego bota
    public float expansionInterval = 5f;

    private Vector3Int spawnPos;
    private Vector3Int currentPos;
    private float timer;
    private bool initialized = false;

    // Czekamy jedn¹ klatkê, ¿eby HexMapGenerator zd¹¿y³ wygenerowaæ mapê w Start()
    private System.Collections.IEnumerator Start()
    {
        if (map == null)
        {
            Debug.LogError("BotController: brak przypisanego HexMapGenerator!");
            yield break;
        }

        yield return null; // jedna klatka

        // wybieramy spawn na podstawie spawnNumber
        if (spawnNumber == 2)
        {
            spawnPos = map.spawnPosPlayer2;
        }
        else
        {
            spawnPos = map.spawnPosPlayer1;
        }

        currentPos = spawnPos;

        // zaznaczamy startowe pole jako teren bota
        map.SetOwnerAndTile(currentPos, botOwnerId, botTile);

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
            ExpandOneStep();
        }
    }

    void ExpandOneStep()
    {
        // 1. Najpierw próbujemy rozszerzyæ siê z aktualnej pozycji
        if (!TryExpandFrom(currentPos))
        {
            // 2. Jeœli siê nie da, spróbuj ze spawna (¿eby bot nie zablokowa³ siê w jakiejœ „dziurze”)
            TryExpandFrom(spawnPos);
        }
    }

    bool TryExpandFrom(Vector3Int from)
    {
        List<Vector3Int> neighbours = map.GetNeighbours(from);

        List<Vector3Int> preferred = new List<Vector3Int>(); // neutralne / cudze
        List<Vector3Int> backup = new List<Vector3Int>();    // nasze w³asne pola

        foreach (var n in neighbours)
        {
            if (!map.IsPassableLand(n))
                continue;

            int owner = map.GetOwnerId(n);

            if (owner != botOwnerId)
                preferred.Add(n);  // najpierw chcemy neutral/enemy
            else
                backup.Add(n);     // potem mo¿emy wejœæ na swoje
        }

        Vector3Int? targetNullable = null;

        if (preferred.Count > 0)
        {
            targetNullable = preferred[Random.Range(0, preferred.Count)];
        }
        else if (backup.Count > 0)
        {
            targetNullable = backup[Random.Range(0, backup.Count)];
        }

        if (targetNullable.HasValue)
        {
            Vector3Int target = targetNullable.Value;

            // przejmujemy pole (albo odœwie¿amy swoje)
            map.SetOwnerAndTile(target, botOwnerId, botTile);

            // bot „idzie” w to miejsce
            currentPos = target;
            return true;
        }

        // nie ma ¿adnych s¹siadów do ruchu
        return false;
    }
}
