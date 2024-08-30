using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

public class LRWar : LRBase
{
    public LRWar(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        // give weapon health and plenty of ammo
        weaponRestrict = Weapon.GUN_LIST[choice];
  
        player.GiveWeapon(weaponRestrict);

        player.SetHealth(1000);
    }

    public override void WeaponFire(String name)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

        var weapon = player.FindWeapon(name);
        weapon.SetAmmo(999,999);
    }
}