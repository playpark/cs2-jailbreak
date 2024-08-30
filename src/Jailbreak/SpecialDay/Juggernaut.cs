using CounterStrikeSharp.API.Core;

public class SDJuggernaut : SDBase
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

    public override void Death(CCSPlayerController? player, CCSPlayerController? attacker, String weapon)
    {
        if (!player.IsLegal() || !attacker.IsLegalAlive())
            return;

        // Give attacker 100 hp
        attacker.SetHealth(attacker.GetHealth() + 100);
    }

    public override void SetupPlayer(CCSPlayerController? player)
    {
        player.EventGunMenu();
    }
}