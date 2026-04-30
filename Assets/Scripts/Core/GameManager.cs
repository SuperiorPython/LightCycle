using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

// =============================================================================
//  AIController.cs  —  Member 2: AI Enhancements
//  Last updated: 2026
// =============================================================================
//
//  WHAT CHANGED FROM THE ORIGINAL
//  ────────────────────────────────
//  The original AIController used a simple "runway" score — it looked straight
//  ahead in each of 3 directions and picked whichever had the most empty cells
//  in a straight line. It had no awareness of enclosed spaces, no knowledge of
//  opponents, and no variation between AI bikes.
//
//  This rewrite replaces that with four layered systems:
//
//  1. DIFFICULTY LEVELS  (Easy / Medium / Hard / Elite)
//     ─────────────────────────────────────────────────
//     Controlled by the `difficulty` enum set per AI in the Inspector.
//     Easy AIs make random blunders 55% of the time, ignoring the best move
//     and picking any safe direction instead. Elite AIs never blunder and use
//     the deepest pathfinding search (200 cells vs 40 on Easy).
//     Implemented in: MistakeChance[], FillDepth[], MaybeBlunder()
//
//  2. SMARTER PATHFINDING  (Flood Fill + Predictive Intercept)
//     ──────────────────────────────────────────────────────────
//     Replaces the straight-line runway with a BFS flood fill that counts ALL
//     reachable open cells in any direction — not just forward. This stops the
//     AI from turning into dead-end pockets. For Hunt mode, it also predicts
//     where the opponent will be N steps ahead (using their current direction)
//     and steers toward that future position instead of chasing their tail.
//     Implemented in: FloodFill(), TryEnqueue(), HuntDir()
//
//  3. AGGRESSIVE vs DEFENSIVE BEHAVIOURS  (State Machine)
//     ─────────────────────────────────────────────────────
//     The AI evaluates its situation every frame and switches between 4 states:
//       • Survive  — flood fill + heatmap, stays away from danger zones
//       • Hunt     — closes distance to predicted opponent position
//       • Trap     — picks moves that shrink the opponent's reachable space
//       • Panic    — pure flood fill, just tries not to die
//     State is chosen by comparing the AI's open space vs the opponent's open
//     space and the distance between them. A winning AI traps; a losing AI survives.
//     Implemented in: AIState enum, EvaluateState(), ChooseDirection()
//
//  4. PERSONALITY STYLES  (Balanced / Rusher / Coward / Trapper)
//     ─────────────────────────────────────────────────────────────
//     Personalities override how the state machine decides which state to enter,
//     giving each AI a distinct feel without separate classes:
//       • Balanced  — adapts based on space ratio and distance (default)
//       • Rusher    — always enters Hunt when an opponent is within 20 cells
//       • Coward    — never hunts, always prioritises its own survival space
//       • Trapper   — patient, waits until it has a clear space advantage,
//                     then switches to Trap to close the opponent in
//     Implemented in: Personality enum, EvaluateState() switch block
//
//  HOW THE SYSTEMS CONNECT EACH FRAME
//  ────────────────────────────────────
//  Update() runs this pipeline on every frame the bike can accept a new direction:
//
//    BakeHeatmap()        — every 4 frames, radiates danger outward from all
//                           opponent heads into a float[,] grid
//         ↓
//    EvaluateState()      — reads space ratios + distance + personality
//                           to pick Survive / Hunt / Trap / Panic
//         ↓
//    ChooseDirection()    — routes to the matching strategy function,
//                           each of which scores all 3 candidate directions
//                           (straight, turn left, turn right) and returns the best
//         ↓
//    MaybeBlunder()       — on Easy/Medium, randomly overrides the ideal
//                           choice with any safe direction to simulate mistakes
//         ↓
//    bike.QueueDirection()  — feeds the chosen direction into BikeController
//                             to be applied on the next movement tick
//
//  WIRING (done automatically by GameManager.StartMatch())
//  ────────────────────────────────────────────────────────
//  GameManager calls ai.SetOpponents() after spawning all bikes, passing in
//  every other BikeController so the AI knows who to hunt, avoid, and trap.
//  No manual Inspector wiring of opponents is needed.
//
// =============================================================================

public enum GameState
{
    Title,
    Difficulty,
    Countdown,
    Playing,
    GameOver,
    Win
}

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public ArenaGrid arena;
    public BikeController[] bikes;

    [Header("UI")]
    public TitleUI titleUI;
    public DifficultyUI difficultyUI;
    public GameOverUI gameOverUI;
    public WinUI winUI;
    public CountdownUI countdownUI;

    [Header("Scoring")]
    public int killPoints = 50;

    private int[] score;
    private bool[] dead;

    [Header("Match State")]
    public GameState state = GameState.Title;

    [Header("Difficulty")]
    public AIController.Difficulty selectedDifficulty = AIController.Difficulty.Medium;

    [Header("Countdown")]
    public float countdownSeconds = 3f;

    [Header("Spawn Tuning")]
    public int spawnRadius = 12;

    void Start()
    {
        score = new int[bikes.Length];
        dead = new bool[bikes.Length];

        SetState(GameState.Title);
    }

    public void SetState(GameState newState)
    {
        state = newState;

        HideAllUI();

        switch (state)
        {
            case GameState.Title:
                Time.timeScale = 0f;
                FreezeAllBikes();

                if (titleUI != null)
                    titleUI.Show();
                break;

            case GameState.Difficulty:
                Time.timeScale = 0f;
                FreezeAllBikes();

                if (difficultyUI != null)
                    difficultyUI.Show();
                break;

            case GameState.Countdown:
                Time.timeScale = 1f;
                StartMatch();
                StartCoroutine(StartCountdown());
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                EnableAliveBikes();
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                FreezeAllBikes();

                if (gameOverUI != null)
                    gameOverUI.Show();
                break;

            case GameState.Win:
                Time.timeScale = 0f;
                FreezeAllBikes();

                if (winUI != null)
                    winUI.Show();
                break;
        }
    }

    IEnumerator StartCountdown()
    {
        FreezeAllBikes();

        if (countdownUI != null)
            yield return countdownUI.PlayCountdown(countdownSeconds);

        SetState(GameState.Playing);
    }

    void HideAllUI()
    {
        if (titleUI != null) titleUI.Hide();
        if (difficultyUI != null) difficultyUI.Hide();
        if (gameOverUI != null) gameOverUI.Hide();
        if (winUI != null) winUI.Hide();
        if (countdownUI != null) countdownUI.Hide();
    }

    public void StartMatch()
    {
        for (int i = 0; i < bikes.Length; i++)
        {
            score[i] = 0;
            dead[i] = false;
        }

        arena.Clear();

        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] != null && bikes[i].trailManager != null)
                bikes[i].trailManager.ClearAll();
        }

        for (int i = 0; i < bikes.Length; i++)
        {
            BikeController b = bikes[i];
            if (b == null) continue;

            b.bikeId = i;
            b.arena = arena;
            b.gameManager = this;
            b.isPlayer = (i == 0);

            if (b.trailManager != null)
                b.trailManager.ownerId = i;
        }

        Vector2Int[] spawns = GenerateSpawns(bikes.Length);
        Vector2Int[] dirs = GenerateDirs(bikes.Length);

        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] == null) continue;
            bikes[i].Respawn(spawns[i], dirs[i]);
        }

        ApplyAIDifficulty();
        WireAIOpponents();
    }

    void ApplyAIDifficulty()
    {
        for (int i = 1; i < bikes.Length; i++)
        {
            if (bikes[i] == null) continue;

            AIController ai = bikes[i].GetComponent<AIController>();
            if (ai == null) continue;

            ai.difficulty = selectedDifficulty;
        }
    }

    void WireAIOpponents()
    {
        for (int i = 0; i < bikes.Length; i++)
        {
            AIController ai = bikes[i]?.GetComponent<AIController>();
            if (ai == null) continue;

            List<BikeController> others = new List<BikeController>();

            for (int j = 0; j < bikes.Length; j++)
            {
                if (j != i && bikes[j] != null)
                    others.Add(bikes[j]);
            }

            ai.SetOpponents(others.ToArray());
        }
    }

    public void AddSurvivalPoint(int bikeId, int amount)
    {
        if (state != GameState.Playing) return;
        if (bikeId < 0 || bikeId >= score.Length) return;
        if (dead[bikeId]) return;

        int scaled = Mathf.RoundToInt(amount * GetDifficultyMultiplier());
        score[bikeId] += scaled;
    }

    public void OnBikeCrashed(BikeController victim, int killerId)
    {
        if (state != GameState.Playing) return;
        if (victim == null) return;

        int victimId = victim.bikeId;
        if (victimId < 0 || victimId >= bikes.Length) return;
        if (dead[victimId]) return;

        dead[victimId] = true;

        if (killerId >= 0 && killerId < score.Length && killerId != victimId)
        {
            int scaledKill = Mathf.RoundToInt(killPoints * GetDifficultyMultiplier());
            score[killerId] += scaledKill;
        }

        if (victim.trailManager != null)
            victim.trailManager.ClearAll();

        if (victimId == 0)
        {
            Debug.Log("GAME OVER: Player died");
            SetState(GameState.GameOver);
            return;
        }

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
            Debug.Log("YOU WIN: All AIs eliminated");
            SetState(GameState.Win);
        }
    }

    void FreezeAllBikes()
    {
        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] != null)
                bikes[i].enabled = false;
        }
    }

    void EnableAliveBikes()
    {
        for (int i = 0; i < bikes.Length; i++)
        {
            if (bikes[i] != null && !dead[i])
                bikes[i].enabled = true;
        }
    }

    public void PlayFromTitle()
    {
        SetState(GameState.Difficulty);
    }

    public void SelectDifficulty(AIController.Difficulty difficulty)
    {
        selectedDifficulty = difficulty;
        SetState(GameState.Countdown);
    }

    public void Retry()
    {
        SetState(GameState.Difficulty);
    }

    public void ReturnToTitle()
    {
        SetState(GameState.Title);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public int BikeCount => score?.Length ?? 0;

    public int GetScore(int id)
    {
        return score != null && id >= 0 && id < score.Length ? score[id] : 0;
    }

    public bool IsDead(int id)
    {
        return dead != null && id >= 0 && id < dead.Length && dead[id];
    }

    public string GetLabel(int id)
    {
        string[] names =
        {
        "Blue (Player)",
        "Red",
        "Green",
        "Yellow",
        "Pink",
        "Orange"
    };

        if (id >= 0 && id < names.Length)
            return names[id];

        return $"Bike {id}";
    }

    public string FinalScoreString()
    {

        if (score == null) return "";

        string[] colorHex =
        {
        "#00BFFF", // Blue / Player
        "#FF4444", // Red
        "#00FF7F", // Green
        "#FFD700", // Yellow
        "#FF69B4", // Pink
        "#FFA500"  // Orange
    };

        List<(int id, int score)> list = new List<(int id, int score)>();

        for (int i = 0; i < score.Length; i++)
            list.Add((i, score[i]));

        // highest score first
        list.Sort((a, b) => b.score.CompareTo(a.score));

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>Difficulty: {selectedDifficulty}</b>\n");

        bool first = true;

        foreach (var entry in list)
        {
            string label = GetLabel(entry.id);
            string status = IsDead(entry.id) ? "DEAD" : "ALIVE";

            string coloredLabel = label;

            if (entry.id >= 0 && entry.id < colorHex.Length)
                coloredLabel = $"<color={colorHex[entry.id]}>{label}</color>";

            if (first)
            {
                sb.AppendLine($"★ {coloredLabel}: {entry.score} ({status})");
                first = false;
            }
            else
            {
                sb.AppendLine($"{coloredLabel}: {entry.score} ({status})");
            }
        }

        return sb.ToString();
    }

    float GetDifficultyMultiplier()
    {
        switch (selectedDifficulty)
        {
            case AIController.Difficulty.Easy: return 0.75f;
            case AIController.Difficulty.Medium: return 1.0f;
            case AIController.Difficulty.Hard: return 1.4f;
            case AIController.Difficulty.Elite: return 1.8f;
            default: return 1.0f;
        }
    }

    Vector2Int[] GenerateSpawns(int n)
    {
        Vector2Int[] s = new Vector2Int[n];

        int cx = arena.width / 2;
        int cy = arena.height / 2;

        s[0] = new Vector2Int(cx, cy);

        int r = Mathf.Max(6, spawnRadius);

        Vector2Int[] ring =
        {
            new Vector2Int(cx + r, cy),
            new Vector2Int(cx - r, cy),
            new Vector2Int(cx, cy + r),
            new Vector2Int(cx, cy - r),
            new Vector2Int(cx + r, cy + r),
        };

        for (int i = 1; i < n; i++)
            s[i] = ring[(i - 1) % ring.Length];

        return s;
    }

    Vector2Int[] GenerateDirs(int n)
    {
        Vector2Int[] d = new Vector2Int[n];

        d[0] = Vector2Int.right;

        if (n > 1) d[1] = Vector2Int.right;
        if (n > 2) d[2] = Vector2Int.left;
        if (n > 3) d[3] = Vector2Int.up;
        if (n > 4) d[4] = Vector2Int.down;
        if (n > 5) d[5] = Vector2Int.up;

        return d;
    }
}