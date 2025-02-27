using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

public class CTQueue
{
    private readonly Queue<int> queueSlots = new Queue<int>();
    private readonly HashSet<int> queueSet = new HashSet<int>();
    public JailConfig Config { get; set; } = new JailConfig();

    public static String QUEUE_PREFIX = $" {ChatColors.Blue}[CT QUEUE]: {ChatColors.White}";

    public void Clear()
    {
        queueSlots.Clear();
        queueSet.Clear();
    }

    public bool IsInQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return false;

        return queueSet.Contains(player.Slot);
    }

    public void JoinQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (player.IsCt())
        {
            player.Announce(QUEUE_PREFIX, "You are already on the CT team.");
            return;
        }

        if (IsInQueue(player))
        {
            player.Announce(QUEUE_PREFIX, "You are already in the queue.");
            return;
        }

        queueSlots.Enqueue(player.Slot);
        queueSet.Add(player.Slot);

        int position = queueSlots.Count;
        player.Announce(QUEUE_PREFIX, $"You have joined the CT queue. Position: {position}");

        // Calculate estimated wait time based on queue position and team ratio
        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();
        int maxCTs = (tCount / Config.Guard.TeamRatio) + (tCount % Config.Guard.TeamRatio > 0 ? 1 : 0);
        int availableSlots = maxCTs - ctCount;

        if (availableSlots > 0)
        {
            if (position <= availableSlots)
            {
                player.Announce(QUEUE_PREFIX, "You will be moved to CT at the start of the next round!");
            }
            else
            {
                player.Announce(QUEUE_PREFIX, $"There are currently {availableSlots} CT slots available. You'll be moved at the start of the next round if slots remain.");
            }
        }
        else
        {
            player.Announce(QUEUE_PREFIX, "No CT slots available right now. You'll be moved when a slot opens at the start of the next round.");
        }

        Chat.Announce(QUEUE_PREFIX, $"{player.PlayerName} has joined the CT queue.");

        // Don't process queue immediately - only at round start/end
    }

    public void LeaveQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (!IsInQueue(player))
        {
            player.Announce(QUEUE_PREFIX, "You are not in the queue.");
            return;
        }

        // Need to rebuild the queue without this player
        RemovePlayerFromQueue(player.Slot);

        player.Announce(QUEUE_PREFIX, "You have left the CT queue.");
    }

    private void RemovePlayerFromQueue(int slot)
    {
        if (!queueSet.Contains(slot))
            return;

        queueSet.Remove(slot);

        // Rebuild queue without this player
        var tempQueue = new Queue<int>();
        while (queueSlots.Count > 0)
        {
            int currentSlot = queueSlots.Dequeue();
            if (currentSlot != slot)
            {
                tempQueue.Enqueue(currentSlot);
            }
        }

        // Copy elements back to the original queue instead of reassigning
        queueSlots.Clear();
        foreach (var item in tempQueue)
        {
            queueSlots.Enqueue(item);
        }
    }

    public void ListQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (queueSlots.Count == 0)
        {
            player.Announce(QUEUE_PREFIX, "The CT queue is currently empty.");
            return;
        }

        player.Announce(QUEUE_PREFIX, "Current CT Queue:");

        int position = 1;
        foreach (int slot in queueSlots)
        {
            CCSPlayerController? queuedPlayer = Utilities.GetPlayerFromSlot(slot);
            if (queuedPlayer.IsLegal())
            {
                player.Announce(QUEUE_PREFIX, $"{position}. {queuedPlayer.PlayerName}");
            }
            position++;
        }
    }

    public void ProcessQueue(bool isRoundStart = false)
    {
        if (!Config.Guard.Queue.Enabled || queueSlots.Count == 0)
            return;

        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        // Check if we can add more CTs based on the ratio
        int maxCTs = (tCount / Config.Guard.TeamRatio) + (tCount % Config.Guard.TeamRatio > 0 ? 1 : 0);

        // At round start, be more aggressive with filling CT slots
        if (isRoundStart && maxCTs < 2 && tCount > 0)
        {
            maxCTs = 2; // Ensure at least 2 CT slots at round start if there are Ts
        }

        int availableSlots = maxCTs - ctCount;

        if (availableSlots <= 0)
            return;

        // Process players in queue up to available slots
        int processed = 0;
        while (queueSlots.Count > 0 && processed < availableSlots)
        {
            int slot = queueSlots.Peek();
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);

            if (!player.IsLegal() || player.IsCt())
            {
                // Remove invalid players or those already on CT
                queueSlots.Dequeue();
                queueSet.Remove(slot);
                continue;
            }

            // Move player to CT
            player.SwitchTeam(CsTeam.CounterTerrorist);
            Chat.Announce(QUEUE_PREFIX, $"{player.PlayerName} has been moved to CT from the queue.");

            // Remove from queue
            queueSlots.Dequeue();
            queueSet.Remove(slot);
            processed++;
        }

        // Notify remaining players of their updated position
        if (queueSlots.Count > 0)
        {
            int position = 1;
            foreach (int slot in queueSlots)
            {
                CCSPlayerController? queuedPlayer = Utilities.GetPlayerFromSlot(slot);
                if (queuedPlayer.IsLegal())
                {
                    queuedPlayer.Announce(QUEUE_PREFIX, $"Your position in queue: {position}");
                }
                position++;
            }
        }
    }

    public void PlayerDisconnect(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);
        }
    }

    public void RoundStart()
    {
        // Process queue at the start of each round with special round start flag
        ProcessQueue(true);
    }

    public void RoundEnd()
    {
        // Process queue at the end of each round
        ProcessQueue();
    }

    public void TeamChange(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        // If player joined CT, remove them from queue
        if (player.IsCt() && IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);
        }

        // Don't process queue on team changes - only at round start/end
    }
}