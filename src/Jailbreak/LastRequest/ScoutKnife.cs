using CounterStrikeSharp.API.Core;

public class LRScoutKnife : LRBase
{
    public LRScoutKnife(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {
        player.GiveWeapon("knife");
        player.GiveWeapon("ssg08");
        player.SetGravity(0.1f);
    }

    public override bool WeaponEquip(String name) 
    {
        return name.Contains("knife") || name.Contains("ssg08");  
    }
}