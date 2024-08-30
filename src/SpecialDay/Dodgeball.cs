using CounterStrikeSharp.API.Core;

public class SDDodgeball : SDBase
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
        player.GiveWeapon("flashbang");
        weaponRestrict = "flashbang";
    }

    public override void GrenadeThrown(CCSPlayerController? player)
    {
        player.GiveEventNadeDelay(1.4f,"weapon_flashbang");
    }

    public override void PlayerHurt(CCSPlayerController? player,CCSPlayerController? attacker,int damage, int health, int hitgroup)
    {
        if (player.IsLegalAlive())
            player.Slay();
    }

    public override void EntCreated(CEntityInstance entity)
    {
        entity.RemoveDelay(1.4f,"flashbang_projectile");
    }
}