using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class TeamSave
{
    public void Save()
    {
        count = 0;

        // iter over each active player and save the theam they are on
        foreach (CCSPlayerController player in JB.Lib.GetPlayers())
        {
            int team = player.TeamNum;

            if (JB.Lib.IsActiveTeam(team))
            {
                slots[count] = player.Slot;
                teams[count] = team;
                count++;
            }
        }      
    }

    public void Restore()
    {
        // iter over each player and switch to recorded team
        for (int i = 0; i < count; i++)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slots[i]);

            if(!player.IsLegal())
                continue;

            if (JB.Lib.IsActiveTeam(player.TeamNum))
                player.SwitchTeam((CsTeam)teams[i]);
        }

        count = 0;
    }

    int[] slots = new int[64];
    int[] teams = new int[64];

    int count = 0;
};