    
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Numerics;

public class LRRace : LRBase
{
    private Vector3? startPosition;
    private Vector3? endPosition;
    private bool positionsSet = false;
    private CCSPlayerController terroristPlayer;

    public LRRace(LastRequest manager, LastRequest.LRType type, int LRSlot, int playerSlot, String choice) : base(manager, type, LRSlot, playerSlot, choice)
    {
        // Set the terrorist player using the playerSlot from the constructor
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player != null && player.IsValid && player.Team == CsTeam.Terrorist)
        {
            terroristPlayer = player;
        }
    }

    public override void InitPlayer(CCSPlayerController player)
    {
        weaponRestrict = "";

        if (player.IsLegalAlive())
        {
            player.SetHealth(1);

            // Only apply settings if player is the terrorist who can control the settings
            if (player.Team == CsTeam.Terrorist && player.Slot == terroristPlayer?.Slot)
            {
                // Terrorist gets to choose settings
                switch (choice)
                {
                    case "Vanilla":
                        break;

                    case "Low gravity":
                        player.SetGravity(0.6f);
                        break;
                }

                // Prompt terrorist to set positions if not already set
                if (!positionsSet)
                {
                    PromptTerroristForPositions(player);
                }
            }
            else if (positionsSet && player.Team == CsTeam.CounterTerrorist)
            {
                // Counter-terrorist just uses the settings chosen by terrorist
                if (choice == "Low gravity")
                {
                    player.SetGravity(0.6f);
                }
            }
        }
    }

    private void PromptTerroristForPositions(CCSPlayerController player)
    {
        // Send message to terrorist to set positions
        player.PrintToChat($"[LR Race] You need to set the start and end positions for the race.");
        player.PrintToChat($"[LR Race] Use !setstart to set your current position as the starting point.");
        player.PrintToChat($"[LR Race] Use !setend to set your current position as the ending point.");
    }

    public bool SetStartPosition(CCSPlayerController player, Vector3 position)
    {
        // Only allow the terrorist to set positions
        if (player.Slot != terroristPlayer?.Slot)
        {
            player.PrintToChat("[LR Race] Only the Terrorist can set race positions.");
            return false;
        }

        startPosition = position;
        player.PrintToChat("[LR Race] Start position set.");
        CheckPositionsSet();
        return true;
    }

    public bool SetEndPosition(CCSPlayerController player, Vector3 position)
    {
        // Only allow the terrorist to set positions
        if (player.Slot != terroristPlayer?.Slot)
        {
            player.PrintToChat("[LR Race] Only the Terrorist can set race positions.");
            return false;
        }

        endPosition = position;
        player.PrintToChat("[LR Race] End position set.");
        CheckPositionsSet();
        return true;
    }

    private void CheckPositionsSet()
    {
        if (startPosition.HasValue && endPosition.HasValue)
        {
            positionsSet = true;
            terroristPlayer?.PrintToChat("[LR Race] Both positions set. The race is ready to begin!");
            // Here you could add logic to notify all players that the race is ready
        }
    }

    // Add a method to get the positions
    public (Vector3?, Vector3?) GetPositions()
    {
        return (startPosition, endPosition);
    }

    public bool ArePositionsSet()
    {
        return positionsSet;
    }
}
