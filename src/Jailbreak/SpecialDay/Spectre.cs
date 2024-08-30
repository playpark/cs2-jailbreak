using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using JB;
using System.Drawing;

public class SDSpectre : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("sd.damage_enable",delay);
    }

    public override void MakeBoss(CCSPlayerController? spectre, int count)
    {
        if (spectre != null && spectre.IsLegalAlive())
        {
            LocalizeAnnounce($"sd.spectre",spectre.PlayerName);

            // give the spectre the HP and swap him
            spectre.SetHealth(count * 60);
            spectre.SwitchTeam(CsTeam.CounterTerrorist);
            
            SetupPlayer(spectre);
        }

        else Chat.Announce("[ERROR] ","Error picking spectre");
    }

    public override bool WeaponEquip(CCSPlayerController player,String name) 
    {
        // spectre can only carry a knife
        if (IsBoss(player))
            return name.Contains("knife") || name.Contains("decoy");

        return true;
    }

    public override void Start()
    {
        LocalizeAnnounce("sd.fight");
        Lib.SwapAllT();

        (CCSPlayerController? boss, int count) = PickBoss();
        MakeBoss(boss,count);

        Lib.ColorAllPlayerModels(Color.FromArgb(100, 255, 255, 255));
    }

    public override void End()
    {
    }

    public override void SetupPlayer(CCSPlayerController player)
    {
        if (IsBoss(player))
        {
            // invis and speed
            player.SetColour(Color.FromArgb(100,255,255,255));
            player.SetVelocity(2.5f);

            player.StripWeapons();
        }

        else player.EventGunMenu();
    }
}