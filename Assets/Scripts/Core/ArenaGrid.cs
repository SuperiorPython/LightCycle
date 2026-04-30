using UnityEngine;

public class ArenaGrid : MonoBehaviour
{
    public int width = 80;
    public int height = 45;
    public float cellSize = 1f;

    private bool[,] occupied;
    private int[,] owner; // -1 = none, otherwise bikeId

    void Awake()
    {
        occupied = new bool[width, height];
        owner = new int[width, height];
        Clear();
    }

    public void Clear()
    {
        occupied = new bool[width, height];
        owner = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                owner[x, y] = -1;
    }

    public bool InBounds(Vector2Int p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;

    public bool IsOccupied(Vector2Int p) => occupied[p.x, p.y];

    public int GetOwner(Vector2Int p) => owner[p.x, p.y];

    public void SetOccupied(Vector2Int p, bool v, int ownerId = -1)
    {
        occupied[p.x, p.y] = v;
        owner[p.x, p.y] = v ? ownerId : -1;
    }

    public Vector3 GridToWorld(Vector2Int p)
    {
        float x = (p.x - width / 2f + 0.5f) * cellSize;
        float y = (p.y - height / 2f + 0.5f) * cellSize;
        return new Vector3(x, y, 0f);
    }
}
