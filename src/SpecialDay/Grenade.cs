using CounterStrikeSharp.API.Core;

public class SDGrenade : SDBase
{
    public override void Setup()
    {
        LocalizeAnnounce("sd.damage_enable",delay);
    }

    public override void Start()
    {
        LocalizeAnnounce("sd.fight");
    }

    public override void End()
    {
    }

    public override void SetupPlayer(CCSPlayerController player)
    {
        player.StripWeapons(true);
        player.SetHealth(175);
        player.GiveWeapon("hegrenade");
        weaponRestrict = "hegrenade";
    }

    public override void GrenadeThrown(CCSPlayerController? player)
    {
        player.GiveEventNadeDelay(1.4f,"weapon_hegrenade");
    }
}