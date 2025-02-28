using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

public class CTQueue
{
    public static string QUEUE_PREFIX = "queue.queue_prefix";
    private readonly Queue<int> queueSlots = new Queue<int>();
    private readonly HashSet<int> queueSet = new HashSet<int>();
    // Track when players join CT team (slot -> timestamp)
    private readonly Dictionary<int, DateTime> ctJoinTimes = new Dictionary<int, DateTime>();
    // Flag to track if team balance is needed
    private bool needsRebalance = false;
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

    // Public method to track when a player joins CT team
    public void TrackCTJoin(CCSPlayerController? player)
    {
        if (player.IsLegal() && player.IsCt())
        {
            ctJoinTimes[player.Slot] = DateTime.UtcNow;
        }
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

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;

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

        // Allow one CT over the limit (CT+1)
        int availableSlots = (maxCTs + 1) - ctCount;

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

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;

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

    // Check if team rebalancing is needed (when Ts leave)
    private void CheckTeamBalance()
    {
        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        // Calculate max allowed CTs based on current T count
        int maxCTs = (tCount / Config.Guard.TeamRatio) + (tCount % Config.Guard.TeamRatio > 0 ? 1 : 0);

        // Allow one CT over the limit (CT+1)
        maxCTs += 1;

        // If we have more CTs than allowed, we need to rebalance
        if (ctCount > maxCTs && ctCount > 0 && tCount >= 0)
        {
            needsRebalance = true;
        }
    }

    // Rebalance teams by moving newest CTs to T
    private void RebalanceTeams()
    {
        if (!needsRebalance)
            return;

        int ctCount = JB.Lib.CtCount();
        int tCount = JB.Lib.TCount();

        // Calculate max allowed CTs based on current T count
        int maxCTs = (tCount / Config.Guard.TeamRatio) + (tCount % Config.Guard.TeamRatio > 0 ? 1 : 0);

        // Allow one CT over the limit (CT+1)
        maxCTs += 1;

        // If we have more CTs than allowed, move the newest ones to T
        if (ctCount > maxCTs)
        {
            int excessCTs = ctCount - maxCTs;

            // Get all CT players sorted by join time (newest first)
            var ctPlayers = new List<KeyValuePair<int, DateTime>>();
            foreach (var player in JB.Lib.GetPlayers())
            {
                if (player.IsCt() && ctJoinTimes.ContainsKey(player.Slot))
                {
                    ctPlayers.Add(new KeyValuePair<int, DateTime>(player.Slot, ctJoinTimes[player.Slot]));
                }
                else if (player.IsCt() && !ctJoinTimes.ContainsKey(player.Slot))
                {
                    // If we don't have a join time for this CT, add them with current time
                    // This ensures they'll be considered as newest CTs
                    ctJoinTimes[player.Slot] = DateTime.UtcNow;
                    ctPlayers.Add(new KeyValuePair<int, DateTime>(player.Slot, ctJoinTimes[player.Slot]));
                }
            }

            // Sort by join time (newest first)
            ctPlayers.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Move the newest CTs to T
            int moved = 0;
            foreach (var ctPlayer in ctPlayers)
            {
                if (moved >= excessCTs)
                    break;

                CCSPlayerController? player = Utilities.GetPlayerFromSlot(ctPlayer.Key);
                if (player.IsLegal() && player.IsCt())
                {
                    player.SwitchTeam(CsTeam.Terrorist);
                    Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.moved_to_t_balance", player.PlayerName);
                    ctJoinTimes.Remove(player.Slot);
                    moved++;
                }
            }

            if (moved > 0)
            {
                Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.team_rebalanced", moved);
            }
        }

        needsRebalance = false;
    }

    public void PlayerDisconnect(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);
        }

        // If a T player disconnects, check if we need to rebalance teams
        if (player.IsT())
        {
            CheckTeamBalance();
        }

        // Remove from CT join times if they were a CT
        if (player.IsCt() && ctJoinTimes.ContainsKey(player.Slot))
        {
            ctJoinTimes.Remove(player.Slot);
        }
    }

    public void RoundStart()
    {
        // Rebalance teams at round start if needed
        if (needsRebalance)
        {
            RebalanceTeams();
        }

        // Process queue after rebalancing
        ProcessQueue(true);
    }

    public void RoundEnd()
    {
        // Check if we need to rebalance teams for the next round
        CheckTeamBalance();

        // Process queue
        ProcessQueue();
    }

    public void TeamChange(CCSPlayerController? player)
    {
        if (!player.IsLegal() || !Config.Guard.Queue.Enabled)
            return;

        if (player.IsCt() && IsInQueue(player))
        {
            RemovePlayerFromQueue(player.Slot);

            // Track when this player joined CT
            ctJoinTimes[player.Slot] = DateTime.UtcNow;
        }
        else if (player.IsT() && ctJoinTimes.ContainsKey(player.Slot))
        {
            // Remove from CT join times if they switched to T
            ctJoinTimes.Remove(player.Slot);

            // Check if CT team is now empty after this player switched to T
            int ctCount = JB.Lib.CtCount();
            int tCount = JB.Lib.TCount();

            if (ctCount == 0 && tCount > 0)
            {
                // CT team is empty but there are still Ts, end the round
                Chat.LocalizeAnnounce(QUEUE_PREFIX, "queue.ct_team_empty");

                // End the current round and skip to the next one
                var GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules;
                Server.NextWorldUpdate(() =>
                {
                    GameRules?.TerminateRound(0, RoundEndReason.RoundDraw);
                });
            }
        }

        // Check if we need to rebalance teams after team change
        CheckTeamBalance();
    }
}