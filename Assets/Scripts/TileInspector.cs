using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class TileInspector : MonoBehaviour
{
    public HexMapGenerator map;
    public Camera cam;

    [Header("Debug (podgl¹d w Inspectorze)")]
    public Vector3Int selectedCoord;
    public bool hasSelection;

    public int ownerId;
    public bool isWater;
    public bool passable;
    public bool hasMine;
    public bool isSpawn;
    public int populationNumber;

    void Update()
    {
        if (map == null) return;
        if (cam == null) cam = Camera.main;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0f;

            Vector3Int cellPos = map.tilemap.WorldToCell(mouseWorld);

            if (map.TryGetCell(cellPos, out HexCell cell))
            {
                hasSelection = true;
                selectedCoord = cellPos;

                ownerId = cell.ownerId;
                isWater = cell.isWater;
                passable = cell.passable;
                hasMine = cell.hasMine;
                isSpawn = cell.isSpawn;
                populationNumber = cell.populationNumber;

                Debug.Log($"Selected {cellPos} | pop={populationNumber} | mine={hasMine} | owner={ownerId}");
            }
            else
            {
                hasSelection = false;
            }
        }
    }
}
