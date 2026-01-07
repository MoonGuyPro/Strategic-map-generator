using UnityEngine;

[System.Serializable]
public class HexCell
{
    public Vector3Int coord;
    public bool isWater;
    public bool passable;
    public int ownerId;        // 0 = neutral, 1 = player1, 2 = player2...
    public bool hasMine;
    public bool isSpawn;
    public int populationNumber;
    public int army = 0;
}
