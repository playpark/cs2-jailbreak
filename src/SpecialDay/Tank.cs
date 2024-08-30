using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class SDTank : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("sd.damage_enable",delay);
    }

    public override void MakeBoss(CCSPlayerController? tank, int count)
    {
        if (tank != null && tank.IsLegalAlive())
        {
            LocalizeAnnounce($"sd.tank",tank.PlayerName);

            // give the tank the HP and swap him
            tank.SetHealth(count * 100);
            tank.SetColour(JB.Lib.RED);
            tank.SwitchTeam(CsTeam.CounterTerrorist);
        }

        else Chat.Announce("[ERROR]: ","Error picking tank");
    }

    public override void Start()
    {
        LocalizeAnnounce("sd.fight");
        JB.Lib.SwapAllT();

        (CCSPlayerController? boss, int count) = PickBoss();
        MakeBoss(boss,count);
    }

    public override void End()
    {
    }

    public override void SetupPlayer(CCSPlayerController player)
    {
        player.EventGunMenu();
    }
}