using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

public class LRHeadshotOnly : LRBase
{
    public LRHeadshotOnly(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        weaponRestrict = "deagle";

        player.GiveWeapon("deagle");
    }

    public override void PlayerHurt(int health,int damage, int hitgroup) 
    {
        // dont allow damage when its not to head
        if (hitgroup != JB.Lib.HITGROUP_HEAD)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
            player.RestoreHP(damage,health);
        }
    }
}