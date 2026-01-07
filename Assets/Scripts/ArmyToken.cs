using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
public class ArmyToken : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    public void Init(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
    }

    public void TeleportToCell(Tilemap tilemap, Vector3Int cell)
    {
        // dok³adnie œrodek heksa – offset robisz w Inspectorze
        transform.position = tilemap.GetCellCenterWorld(cell);
    }
}
