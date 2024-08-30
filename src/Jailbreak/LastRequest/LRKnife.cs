using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

public class LRKnife : LRBase
{
    public LRKnife(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        // give player a knife and restrict them to it
        player.GiveWeapon("knife");
        weaponRestrict = "knife";

        // Handle options
        switch (choice)
        {
            case "Vanilla":
                break;

            case "Low gravity":
            {
                player.SetGravity(0.6f);
                break;
            }

            case "High speed":
            {
                player.SetVelocity(2.5f);
                break;
            }
                
            case "One hit":
            {
                player.SetHealth(50);
                break;
            }
        }
    }

    public override void PlayerHurt(int health,int damage, int hitgroup) 
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

        // re init the player
        if(choice == "High speed" && player.IsLegalAlive())
            player.SetVelocity(2.5f);  
    }
}