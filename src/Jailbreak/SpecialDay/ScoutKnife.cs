// base lr class
using CounterStrikeSharp.API.Core;

public class SDScoutKnife : SDBase
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
        player.StripWeapons();
        player.GiveWeapon("ssg08");
        player.SetGravity(0.1f);
    }

    public override bool WeaponEquip(CCSPlayerController player,String name) 
    {
        return name.Contains("knife") || name.Contains("ssg08");
    }

    public override void CleanupPlayer(CCSPlayerController player)
    {
        player.SetGravity(1.0f);
    }
}