// base lr class
using CounterStrikeSharp.API.Core;

public class SDFriendlyFire : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("sd.damage_enable",delay);
    }

    public override void Start()
    {
        LocalizeAnnounce("sd.ffd_enable");
        JB.Lib.EnableFriendlyFire();
    }

    public override void End()
    {
    }

    public override void SetupPlayer(CCSPlayerController? player)
    {
        player.EventGunMenu();
    }
}