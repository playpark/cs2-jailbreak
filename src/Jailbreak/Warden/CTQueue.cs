using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

public class CTQueue
{
    public static string QUEUE_PREFIX = "queue.queue_prefix";
    private readonly Queue<int> queueSlots = new Queue<int>();
    private readonly HashSet<int> queueSet = new HashSet<int>();
    public JailConfig Config { get; set; } = new JailConfig();

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
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.already_ct");
            return;
        }

        if (IsInQueue(player))
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.already_in_queue");
            return;
        }

        queueSlots.Enqueue(player.Slot);
        queueSet.Add(player.Slot);

        int position = queueSlots.Count;
        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.joined", position);

        // Calculate estimated wait time based on queue position and team ratio
        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();
        int maxCTs = (tCount / Config.Guard.TeamRatio) + (tCount % Config.Guard.TeamRatio > 0 ? 1 : 0);
        int availableSlots = maxCTs - ctCount;

        // If there are no CTs at all, process the queue immediately
        if (ctCount == 0 && tCount > 0)
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.processing_immediately");
            ProcessQueue(true);
            // End the current round and skip to the next one
            var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
            Server.NextWorldUpdate(() =>
            {
                GameRules?.TerminateRound(0, RoundEndReason.RoundDraw);
            });
            return;
        }

        // Special case: If both teams are empty, allow the first player to join CT
        if (ctCount == 0 && tCount == 0)
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.processing_immediately");
            player.SwitchTeam(CsTeam.CounterTerrorist);
            Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_to_ct", player.PlayerName);

            // Remove from queue since they're now on CT
            queueSlots.Dequeue();
            queueSet.Remove(player.Slot);
            return;
        }

        if (availableSlots > 0)
        {
            if (position <= availableSlots)
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_next_round");
            }
            else
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.slots_available", availableSlots);
            }
        }
        else
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.no_slots");
        }
    }

    public void LeaveQueue(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (!IsInQueue(player))
        {
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.not_in_queue");
            return;
        }

        // Need to rebuild the queue without this player
        RemovePlayerFromQueue(player.Slot);

        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.left");
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
            player.LocalizeAnnounce(QUEUE_PREFIX, "queue.empty");
            return;
        }

        player.LocalizeAnnounce(QUEUE_PREFIX, "queue.current");

        int position = 1;
        foreach (int slot in queueSlots)
        {
            CCSPlayerController? queuedPlayer = Utilities.GetPlayerFromSlot(slot);
            if (queuedPlayer.IsLegal())
            {
                player.LocalizeAnnounce(QUEUE_PREFIX, "queue.position_entry", position, queuedPlayer.PlayerName);
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

        // Special case: If both teams are empty, allow at least one CT
        if (ctCount == 0 && tCount == 0)
        {
            maxCTs = 1; // Allow at least one CT when both teams are empty
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
            Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_to_ct", player.PlayerName);

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
                    queuedPlayer.LocalizeAnnounce(QUEUE_PREFIX, "queue.position_update", position);
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
        ProcessQueue(true);
    }

    public void RoundEnd()
    {
        ProcessQueue();
    }

    public void TeamChange(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (player.IsCt() && IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);
        }
    }
}