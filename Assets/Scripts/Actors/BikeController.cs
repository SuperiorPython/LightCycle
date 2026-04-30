using System;
using UnityEngine;

public class BikeController : MonoBehaviour
{
    // Fired when this bike crashes (player or AI)
    public event Action<BikeController> OnCrashed;

    [Header("Refs")]
    public ArenaGrid arena;
    public TrailManager trailManager;
    public GameManager gameManager;

    [Header("Identity")]
    public int bikeId = -1;         // set by GameManager
    public bool isPlayer = false;   // set true for PlayerBike (or set by GameManager)

    [Header("Grid State")]
    public Vector2Int gridPos;
    public Vector2Int dir = Vector2Int.right;

    [Header("Timing")]
    public float ticksPerSecond = 18f;

    // queued direction applied on tick (prevents jitter)
    Vector2Int pendingDir;
    bool hasPendingDir = false;

    float tickTimer;
    bool alive = true;
    public bool IsAlive => alive;


    public void Respawn(Vector2Int startPos, Vector2Int startDir)
    {
        alive = true;
        enabled = true;

        gridPos = startPos;
        dir = startDir;

        hasPendingDir = false;
        tickTimer = 0f;

        transform.position = arena.GridToWorld(gridPos);
        transform.rotation = Quaternion.Euler(0, 0, DirToAngle(dir));

        // mark starting cell occupied & owned (so you can't overlap spawns)
        arena.SetOccupied(gridPos, true, bikeId);
    }

    // --------- Player input (WASD + arrows, absolute direction, no reverse) ---------
    void Update()
    {
        if (!isPlayer) return;     // AI bikes ignore keyboard input
        if (!alive) return;
        if (hasPendingDir) return; // one change per tick

        Vector2Int desired = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            desired = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            desired = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            desired = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            desired = Vector2Int.right;

        if (desired == Vector2Int.zero) return;
        if (desired == -dir) return; // block reversal

        pendingDir = desired;
        hasPendingDir = true;
    }

    // --------- AI helper hooks (used by AIController) ---------
    public bool CanQueueDirection() => alive && !hasPendingDir;

    public void QueueDirection(Vector2Int desired)
    {
        if (!alive) return;
        if (hasPendingDir) return;
        if (desired == Vector2Int.zero) return;
        if (desired == -dir) return; // block reversal

        pendingDir = desired;
        hasPendingDir = true;
    }

    // --------- Movement tick ---------
    void FixedUpdate()
    {
        if (!alive) return;

        tickTimer += Time.fixedDeltaTime;
        float tick = 1f / ticksPerSecond;

        if (tickTimer >= tick)
        {
            tickTimer -= tick;

            if (hasPendingDir)
            {
                dir = pendingDir;
                hasPendingDir = false;
            }

            Step();
        }
    }

    void Step()
    {
        Vector2Int next = gridPos + dir;

        // wall collision => no killer
        if (!arena.InBounds(next))
        {
            Crash(-1);
            return;
        }

        // trail collision => killer is owner of the cell
        if (arena.IsOccupied(next))
        {
            int owner = arena.GetOwner(next); // requires updated ArenaGrid
            Crash(owner);
            return;
        }

        // leave trail on current cell (TrailManager should stamp ownership too)
        trailManager.SpawnAt(gridPos);

        // move into next cell (mark occupied & owned)
        gridPos = next;
        arena.SetOccupied(gridPos, true, bikeId);

        transform.position = arena.GridToWorld(gridPos);
        transform.rotation = Quaternion.Euler(0, 0, DirToAngle(dir));

        // passive survival points per successful step
        gameManager?.AddSurvivalPoint(bikeId, 1);
    }

    void Crash(int killerId)
    {
        alive = false;
        enabled = false;

        OnCrashed?.Invoke(this);

        if (gameManager != null)
            gameManager.OnBikeCrashed(this, killerId);
    }

    static float DirToAngle(Vector2Int d)
        => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
}
