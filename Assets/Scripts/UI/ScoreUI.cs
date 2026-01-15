using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public GameManager gm;
    public Text text;

    // slots[top..bottom] store bikeIds in display order
    private List<int> slots = new List<int>();

    // remembers which dead bikes have been "sent to bottom" already
    private HashSet<int> deadLocked = new HashSet<int>();

    void Start()
    {
        InitializeSlots();
    }

    void Update()
    {
        if (gm == null || text == null) return;

        if (slots.Count != gm.BikeCount)
            InitializeSlots();

        // 1) If someone just died, move them once to the lowest free slot
        PushNewlyDeadToBottom();

        // 2) Reorder ONLY the alive entries into the remaining slots by score
        ReorderAliveInFreeSlots();

        // 3) Render text (dead = red)
        Render();
    }

    void InitializeSlots()
    {
        slots.Clear();
        deadLocked.Clear();

        int n = gm != null ? gm.BikeCount : 0;
        for (int i = 0; i < n; i++)
            slots.Add(i);
    }

    void PushNewlyDeadToBottom()
    {
        int n = gm.BikeCount;

        // Find any dead bikes that haven't been locked to the bottom yet
        for (int id = 0; id < n; id++)
        {
            if (!gm.IsDead(id)) continue;
            if (deadLocked.Contains(id)) continue;

            int fromSlot = FindSlotOf(id);
            if (fromSlot == -1) continue;

            // Find bottom-most slot that is NOT currently occupied by a dead bike
            int targetSlot = FindBottomMostNonDeadSlot();
            if (targetSlot == -1) targetSlot = fromSlot; // fallback

            // If target is same slot, just lock it
            if (targetSlot != fromSlot)
            {
                int displaced = slots[targetSlot];
                slots[targetSlot] = id;
                slots[fromSlot] = displaced;
            }

            deadLocked.Add(id);
        }
    }

    int FindBottomMostNonDeadSlot()
    {
        for (int s = slots.Count - 1; s >= 0; s--)
        {
            int bikeId = slots[s];
            if (!gm.IsDead(bikeId)) return s;
        }
        return -1;
    }

    int FindSlotOf(int bikeId)
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] == bikeId) return i;
        return -1;
    }

    void ReorderAliveInFreeSlots()
    {
        int n = gm.BikeCount;

        // Determine which slots are "fixed" because they contain a locked dead bike
        bool[] fixedSlot = new bool[n];
        for (int slot = 0; slot < n; slot++)
        {
            int id = slots[slot];
            if (gm.IsDead(id) && deadLocked.Contains(id))
                fixedSlot[slot] = true;
        }

        // Collect alive bikes
        List<int> alive = new List<int>(n);
        for (int id = 0; id < n; id++)
            if (!gm.IsDead(id))
                alive.Add(id);

        // Sort alive by score desc, tie-break by id asc
        alive.Sort((a, b) =>
        {
            int sb = gm.GetScore(b);
            int sa = gm.GetScore(a);
            if (sb != sa) return sb.CompareTo(sa);
            return a.CompareTo(b);
        });

        // Fill all non-fixed slots from top to bottom with alive list
        int aliveIndex = 0;
        for (int slot = 0; slot < n; slot++)
        {
            if (fixedSlot[slot]) continue;

            if (aliveIndex < alive.Count)
                slots[slot] = alive[aliveIndex++];
        }
    }

    void Render()
    {
        text.supportRichText = true;

        StringBuilder sb = new StringBuilder(256);
        for (int slot = 0; slot < slots.Count; slot++)
        {
            int id = slots[slot];
            string line = $"{gm.GetLabel(id)}: {gm.GetScore(id)}";

            if (gm.IsDead(id))
                line = $"<color=#FF3B3B>{line}</color>";

            sb.AppendLine(line);
        }

        text.text = sb.ToString();
    }
}
