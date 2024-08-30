// base lr class
using CounterStrikeSharp.API.Core;

public class SDHeadshotOnly : SDBase
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
        player.GiveWeapon("deagle");
        weaponRestrict = "deagle";
    }

    public override void PlayerHurt(CCSPlayerController? player,CCSPlayerController? attacker,int health,int damage, int hitgroup) 
    {
        if (!player.IsLegalAlive())
            return;

        // dont allow damage when its not to head
        if (hitgroup != JB.Lib.HITGROUP_HEAD)
           player.RestoreHP(damage,health);
    }
}