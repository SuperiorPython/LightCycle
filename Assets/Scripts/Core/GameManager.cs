using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public ArenaGrid arena;
    public BikeController[] bikes; // Set size = 6 (Player + 5 AI) in Inspector

    [Header("Scoring")]
    public int killPoints = 50;

    private int[] score;
    private bool[] dead;

    [Header("Match State")]
    public bool matchOver = false;

    [Header("Spawn Tuning")]
    public int spawnRadius = 12;

    void Start()
    {
        // init arrays
        score = new int[bikes.Length];
        dead = new bool[bikes.Length];

        StartMatch();
    }

    public void StartMatch()
    {
        matchOver = false;

        // reset scores/deaths (comment out score reset if you want persistent match scoring)
        for (int i = 0; i < bikes.Length; i++)
        {
            score[i] = 0;
            dead[i] = false;
        }

        // reset arena + clear all trails
        arena.Clear();
        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] != null && bikes[i].trailManager != null)
                bikes[i].trailManager.ClearAll();
        }

        // auto-wire IDs + refs + control flags + trail ownership
        for (int i = 0; i < bikes.Length; i++)
        {
            var b = bikes[i];
            if (b == null) continue;

            b.bikeId = i;
            b.arena = arena;
            b.gameManager = this;
            b.isPlayer = (i == 0);

            // IMPORTANT: needed for elimination credit (ArenaGrid owner tracking)
            if (b.trailManager != null)
                b.trailManager.ownerId = i;
        }

        // spawn everyone
        Vector2Int[] spawns = GenerateSpawns(bikes.Length);
        Vector2Int[] dirs = GenerateDirs(bikes.Length);

        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] == null) continue;
            bikes[i].Respawn(spawns[i], dirs[i]);
        }
    }

    // Called by BikeController each successful step
    public void AddSurvivalPoint(int bikeId, int amount)
    {
        if (matchOver) return;
        if (bikeId < 0 || bikeId >= score.Length) return;

        // Only alive bikes accrue survival
        if (dead[bikeId]) return;

        score[bikeId] += amount;
    }

    // Called by BikeController when it crashes
    public void OnBikeCrashed(BikeController victim, int killerId)
    {
        if (matchOver) return;
        if (victim == null) return;

        int victimId = victim.bikeId;
        if (victimId < 0 || victimId >= bikes.Length) return;

        // If already dead, ignore duplicate crash calls
        if (dead[victimId]) return;

        // mark dead (this is what your UI will use to lock + turn red)
        dead[victimId] = true;

        // award kill points (only if someone else owns the cell)
        if (killerId >= 0 && killerId < score.Length && killerId != victimId)
            score[killerId] += killPoints;

        // delete dead trails (your choice) — clears visuals and, if your TrailManager tracks cells,
        // also frees those cells in the ArenaGrid
        if (victim.trailManager != null)
            victim.trailManager.ClearAll();

        // player died => lose
        if (victimId == 0)
        {
            matchOver = true;
            Debug.Log("GAME OVER: Player died");
            FreezeAllBikes();
            return;
        }

        // if all AIs dead => win
        bool anyAIAlive = false;
        for (int i = 1; i < bikes.Length; i++)
        {
            if (!dead[i])
            {
                anyAIAlive = true;
                break;
            }
        }

        if (!anyAIAlive)
        {
            matchOver = true;
            Debug.Log("YOU WIN: All AIs eliminated");
            FreezeAllBikes();
        }
    }

    void FreezeAllBikes()
    {
        for (int i = 0; i < bikes.Length; i++)
            if (bikes[i] != null)
                bikes[i].enabled = false;
    }

    // ---------- UI Helpers (used by your ScoreUI reordering/locking) ----------
    public int BikeCount => score?.Length ?? 0;

    public int GetScore(int id) =>
        (score != null && id >= 0 && id < score.Length) ? score[id] : 0;

    public bool IsDead(int id) =>
        (dead != null && id >= 0 && id < dead.Length) && dead[id];

    public string GetLabel(int id) =>
        (id == 0) ? "Player" : $"AI {id}";

    // ---------- Spawn Helpers (Player + 5 AI = 6 total) ----------
    Vector2Int[] GenerateSpawns(int n)
    {
        Vector2Int[] s = new Vector2Int[n];

        int cx = arena.width / 2;
        int cy = arena.height / 2;

        // Player in center
        s[0] = new Vector2Int(cx, cy);

        int r = Mathf.Max(6, spawnRadius);

        // 5 AI spawn points around player
        Vector2Int[] ring =
        {
            new Vector2Int(cx + r, cy),      // right
            new Vector2Int(cx - r, cy),      // left
            new Vector2Int(cx, cy + r),      // up
            new Vector2Int(cx, cy - r),      // down
            new Vector2Int(cx + r, cy + r),  // up-right
        };

        for (int i = 1; i < n; i++)
            s[i] = ring[(i - 1) % ring.Length];

        return s;
    }

    Vector2Int[] GenerateDirs(int n)
    {
        Vector2Int[] d = new Vector2Int[n];

        d[0] = Vector2Int.right; // player

        // AIs face away from center-ish to reduce instant collisions
        if (n > 1) d[1] = Vector2Int.right; // right spawn -> go right
        if (n > 2) d[2] = Vector2Int.left;  // left spawn  -> go left
        if (n > 3) d[3] = Vector2Int.up;    // up spawn    -> go up
        if (n > 4) d[4] = Vector2Int.down;  // down spawn  -> go down
        if (n > 5) d[5] = Vector2Int.up;    // up-right spawn -> go up (or right)

        return d;
    }
}
