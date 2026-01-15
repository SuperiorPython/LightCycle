using UnityEngine;

public class AIController : MonoBehaviour
{
    public BikeController bike;
    public ArenaGrid arena;

    void Reset()
    {
        bike = GetComponent<BikeController>();
    }

    void Update()
    {
        if (bike == null || arena == null) return;
        if (!bike.CanQueueDirection()) return;

        Vector2Int dir = bike.dir;
        Vector2Int pos = bike.gridPos;

        // Candidate directions: straight, left, right
        Vector2Int straight = dir;
        Vector2Int left = new Vector2Int(-dir.y, dir.x);
        Vector2Int right = new Vector2Int(dir.y, -dir.x);

        Vector2Int best = PickBest(pos, straight, left, right);
        if (best == -bike.dir) best = bike.dir;
        bike.QueueDirection(best);
    }

    Vector2Int PickBest(Vector2Int pos, Vector2Int straight, Vector2Int left, Vector2Int right)
    {
        // Prefer safe direction with the longest "runway" before hitting something
        int sScore = Score(pos, straight);
        int lScore = Score(pos, left);
        int rScore = Score(pos, right);

        // If all are dead, just keep straight (you’re doomed anyway)
        if (sScore < 0 && lScore < 0 && rScore < 0) return straight;

        // Pick max score; tie-break: straight > left > right
        int bestScore = sScore;
        Vector2Int bestDir = straight;

        if (lScore > bestScore) { bestScore = lScore; bestDir = left; }
        if (rScore > bestScore) { bestScore = rScore; bestDir = right; }

        return bestDir;
    }

    int Score(Vector2Int pos, Vector2Int dir)
    {
        Vector2Int next = pos + dir;

        // immediate death = invalid
        if (!arena.InBounds(next) || arena.IsOccupied(next)) return -1;

        // runway: how many cells can we go before collision (cap it for speed)
        int runway = 0;
        Vector2Int p = next;
        for (int i = 0; i < 30; i++)
        {
            if (!arena.InBounds(p) || arena.IsOccupied(p)) break;
            runway++;
            p += dir;
        }

        return runway;
    }
}
