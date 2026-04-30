using System.Collections.Generic;
using UnityEngine;

public class TrailManager : MonoBehaviour
{
    public Transform trailRoot;
    public GameObject trailPrefab;
    public ArenaGrid arena;
    public int ownerId = -1;


    // track the cells this trail occupies
    private readonly List<Vector2Int> cells = new List<Vector2Int>(2048);

    void Awake()
    {
        if (trailRoot == null)
            trailRoot = transform;
    }

    public void SpawnAt(Vector2Int gridPos)
    {
        // mark this cell as owned by this bike
        arena.SetOccupied(gridPos, true, ownerId);

        // track it so ClearAll can free it later
        cells.Add(gridPos);

        Vector3 world = arena.GridToWorld(gridPos);
        Instantiate(trailPrefab, world, Quaternion.identity, trailRoot);
    }


    public void ClearAll()
    {
        // free grid cells
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int p = cells[i];
            if (arena.InBounds(p))
                arena.SetOccupied(p, false);
        }
        cells.Clear();

        // remove visuals
        for (int i = trailRoot.childCount - 1; i >= 0; i--)
            Destroy(trailRoot.GetChild(i).gameObject);
    }
}
