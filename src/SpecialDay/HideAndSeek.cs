using CounterStrikeSharp.API.Core;
using JB;
using System.Drawing;


public class SDHideAndSeek : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("sd.t_hide",delay);
        Lib.ColorAllPlayerModels(Color.FromArgb(100, 255, 255, 255));
    }

    public override void Start()
    {
        // unfreeze all players
        foreach (CCSPlayerController? player in Lib.GetAlivePlayers())
        {
            if (player.IsT())
            {
                player.GiveWeapon("knife");
                player.SetColour(Color.FromArgb(100, 255, 255, 255));
            }

            player.UnFreeze();
        }

        LocalizeAnnounce("sd.seeker_release");
    }

    public override void End()
    {
    }

    public override void SetupPlayer(CCSPlayerController player)
    {
        // lock them in place 500 hp, gun menu
        if (player.IsCt())
        {
            player.Freeze();
            player.EventGunMenu();
            player.SetHealth(500);
        }

        // invis
        else
        {
            player.SetColour(Color.FromArgb(100,255,255,255));
            player.StripWeapons(true);
        }
    }

    public override void CleanupPlayer(CCSPlayerController player)
    {
        player.UnFreeze();
    }
}