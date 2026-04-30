using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────
//  AIController.cs  — Member 2: AI Enhancements
//  Verified against: ArenaGrid.cs, BikeController.cs, GameManager.cs
// ─────────────────────────────────────────────────────────────

public class AIController : MonoBehaviour
{
    // ── 1. DIFFICULTY ────────────────────────────────────────
    public enum Difficulty { Easy, Medium, Hard, Elite }

    [Header("Difficulty")]
    public Difficulty difficulty = Difficulty.Medium;

    static readonly float[] MistakeChance = { 0.55f, 0.25f, 0.08f, 0.0f };
    static readonly int[]   FillDepth     = { 40,    80,    120,   200  };

    // ── 2. PERSONALITY ───────────────────────────────────────
    public enum Personality { Balanced, Rusher, Coward, Trapper }

    [Header("Personality")]
    public Personality personality = Personality.Balanced;

    // ── 3. REFS ──────────────────────────────────────────────
    [Header("Refs")]
    public BikeController bike;
    public ArenaGrid      arena;
    public BikeController[] opponents; // wired by GameManager.StartMatch()

    // ── Internal state ───────────────────────────────────────
    enum AIState { Survive, Hunt, Trap, Panic }
    AIState state;

    float[,] heatmap;
    int      heatTick;

    // ─────────────────────────────────────────────────────────
    void Reset() { bike = GetComponent<BikeController>(); }

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (bike == null || arena == null) return;
        if (!bike.CanQueueDirection()) return;

        // Rebake heatmap every 4 frames
        if (heatTick++ % 4 == 0) BakeHeatmap();

        Vector2Int dir      = bike.dir;
        Vector2Int pos      = bike.gridPos;
        Vector2Int straight = dir;
        Vector2Int left     = new Vector2Int(-dir.y,  dir.x);
        Vector2Int right    = new Vector2Int( dir.y, -dir.x);
        Vector2Int[] cands  = { straight, left, right };

        state = EvaluateState(pos);

        Vector2Int ideal = ChooseDirection(pos, cands);
        Vector2Int final = MaybeBlunder(ideal, cands);

        bike.QueueDirection(final);
    }

    // ── Called by GameManager after all bikes are spawned ────
    public void SetOpponents(BikeController[] others)
    {
        opponents = others;
    }

    // ═════════════════════════════════════════════════════════
    //  STATE MACHINE
    // ═════════════════════════════════════════════════════════
    AIState EvaluateState(Vector2Int pos)
    {
        int mySpace = FloodFill(pos);
        if (mySpace < 12) return AIState.Panic;

        BikeController opp = NearestAliveOpponent();
        if (opp == null) return AIState.Survive;

        float dist     = Vector2Int.Distance(pos, opp.gridPos);
        int   oppSpace = FloodFill(opp.gridPos);

        switch (personality)
        {
            case Personality.Rusher:
                return dist < 20 ? AIState.Hunt : AIState.Survive;

            case Personality.Coward:
                // always prioritises escape — never hunts
                return AIState.Survive;

            case Personality.Trapper:
                return mySpace > oppSpace * 1.5f ? AIState.Trap : AIState.Survive;

            case Personality.Balanced:
            default:
                if (mySpace < 20)            return AIState.Panic;
                if (mySpace > oppSpace * 2f) return AIState.Trap;
                if (dist < 12)               return AIState.Hunt;
                return AIState.Survive;
        }
    }

    // ═════════════════════════════════════════════════════════
    //  DIRECTION CHOOSER
    // ═════════════════════════════════════════════════════════
    Vector2Int ChooseDirection(Vector2Int pos, Vector2Int[] cands)
    {
        BikeController opp = NearestAliveOpponent();

        switch (state)
        {
            case AIState.Panic:   return BestByFloodFill(pos, cands);
            case AIState.Hunt:    return HuntDir(pos, cands, opp);
            case AIState.Trap:    return TrapDir(pos, cands, opp);
            case AIState.Survive:
            default:              return DefensiveDir(pos, cands);
        }
    }

    // ═════════════════════════════════════════════════════════
    //  STRATEGIES
    // ═════════════════════════════════════════════════════════

    // Survive — flood fill minus heatmap danger
    Vector2Int DefensiveDir(Vector2Int pos, Vector2Int[] cands)
    {
        Vector2Int best      = cands[0];
        float      bestScore = float.MinValue;

        foreach (var d in cands)
        {
            Vector2Int next = pos + d;
            if (!arena.InBounds(next) || arena.IsOccupied(next)) continue;

            float score = FloodFill(next) - GetHeat(next) * 1.5f;
            if (score > bestScore) { bestScore = score; best = d; }
        }
        return best;
    }

    // Hunt — close distance to predicted opponent position
    Vector2Int HuntDir(Vector2Int pos, Vector2Int[] cands, BikeController opp)
    {
        if (opp == null) return DefensiveDir(pos, cands);

        int        lookahead = (difficulty == Difficulty.Elite) ? 6 : 3;
        Vector2Int target    = opp.gridPos + opp.dir * lookahead;

        Vector2Int best      = cands[0];
        float      bestScore = float.MinValue;

        foreach (var d in cands)
        {
            Vector2Int next = pos + d;
            if (!arena.InBounds(next) || arena.IsOccupied(next)) continue;

            float space  =  FloodFill(next);
            float aggro  = -Vector2Int.Distance(next, target);
            float score  =  space * 0.4f + aggro * 3f;

            if (score > bestScore) { bestScore = score; best = d; }
        }
        return best;
    }

    // Trap — pick move that shrinks opponent's reachable space
    Vector2Int TrapDir(Vector2Int pos, Vector2Int[] cands, BikeController opp)
    {
        if (opp == null) return DefensiveDir(pos, cands);

        Vector2Int best      = cands[0];
        float      bestScore = float.MinValue;

        foreach (var d in cands)
        {
            Vector2Int next = pos + d;
            if (!arena.InBounds(next) || arena.IsOccupied(next)) continue;

            float mySpace  =  FloodFill(next);
            float oppSpace = -FloodFill(opp.gridPos);
            float score    =  mySpace * 0.5f + oppSpace * 2f;

            if (score > bestScore) { bestScore = score; best = d; }
        }
        return best;
    }

    // Panic — raw flood fill only
    Vector2Int BestByFloodFill(Vector2Int pos, Vector2Int[] cands)
    {
        Vector2Int best      = cands[0];
        int        bestScore = -1;

        foreach (var d in cands)
        {
            Vector2Int next = pos + d;
            if (!arena.InBounds(next) || arena.IsOccupied(next)) continue;
            int s = FloodFill(next);
            if (s > bestScore) { bestScore = s; best = d; }
        }
        return best;
    }

    // ═════════════════════════════════════════════════════════
    //  FLOOD FILL
    //  Uses arena.InBounds + arena.IsOccupied — both confirmed in ArenaGrid.cs
    // ═════════════════════════════════════════════════════════
    int FloodFill(Vector2Int start)
    {
        int cap = FillDepth[(int)difficulty];

        if (!arena.InBounds(start) || arena.IsOccupied(start)) return 0;

        var visited = new HashSet<Vector2Int>();
        var queue   = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0 && visited.Count < cap)
        {
            Vector2Int cur = queue.Dequeue();
            TryEnqueue(cur + Vector2Int.up,    visited, queue);
            TryEnqueue(cur + Vector2Int.down,  visited, queue);
            TryEnqueue(cur + Vector2Int.left,  visited, queue);
            TryEnqueue(cur + Vector2Int.right, visited, queue);
        }
        return visited.Count;
    }

    void TryEnqueue(Vector2Int n, HashSet<Vector2Int> visited, Queue<Vector2Int> queue)
    {
        if (!visited.Contains(n) && arena.InBounds(n) && !arena.IsOccupied(n))
        {
            visited.Add(n);
            queue.Enqueue(n);
        }
    }

    // ═════════════════════════════════════════════════════════
    //  HEATMAP
    //  Uses arena.width + arena.height — confirmed public int fields in ArenaGrid.cs
    // ═════════════════════════════════════════════════════════
    void BakeHeatmap()
    {
        int w = arena.width;   // ✅ public int width  in ArenaGrid.cs line 4
        int h = arena.height;  // ✅ public int height in ArenaGrid.cs line 5
        heatmap = new float[w, h];

        if (opponents == null) return;

        foreach (var opp in opponents)
        {
            if (opp == null || !opp.IsAlive) continue;  // ✅ IsAlive confirmed in BikeController.cs
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float dist = Vector2Int.Distance(new Vector2Int(x, y), opp.gridPos);
                heatmap[x, y] += Mathf.Max(0f, 8f - dist);
            }
        }
    }

    float GetHeat(Vector2Int pos)
    {
        if (heatmap == null || !arena.InBounds(pos)) return 0f;
        return heatmap[pos.x, pos.y];
    }

    // ═════════════════════════════════════════════════════════
    //  DIFFICULTY MISTAKES
    // ═════════════════════════════════════════════════════════
    Vector2Int MaybeBlunder(Vector2Int ideal, Vector2Int[] cands)
    {
        if (Random.value > MistakeChance[(int)difficulty]) return ideal;

        var safe = new List<Vector2Int>();
        foreach (var d in cands)
        {
            Vector2Int next = bike.gridPos + d;
            if (arena.InBounds(next) && !arena.IsOccupied(next))
                safe.Add(d);
        }
        return safe.Count > 0 ? safe[Random.Range(0, safe.Count)] : ideal;
    }

    // ═════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════
    BikeController NearestAliveOpponent()
    {
        if (opponents == null || opponents.Length == 0) return null;

        BikeController best     = null;
        float          bestDist = float.MaxValue;

        foreach (var opp in opponents)
        {
            if (opp == null || !opp.IsAlive) continue;
            float d = Vector2Int.Distance(bike.gridPos, opp.gridPos);
            if (d < bestDist) { bestDist = d; best = opp; }
        }
        return best;
    }
}