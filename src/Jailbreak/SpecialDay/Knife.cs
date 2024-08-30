using CounterStrikeSharp.API.Core;

public class SDKnifeWarday : SDBase
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
        weaponRestrict = "knife";
    }
}