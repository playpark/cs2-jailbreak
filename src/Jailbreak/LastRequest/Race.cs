using CounterStrikeSharp.API.Core;

public class LRRace : LRBase
{
    public LRRace(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        weaponRestrict = "";
   
    {
        

        if (player.IsLegalAlive())
        {
            player.SetHealth(1);



            switch (choice)
            {
                case "Vanilla":
                    break;

                case "Low gravity":
                    player.SetGravity(0.6f);
                    break;
            }
        }
    }    

        
        

        
        

        
        
    }

    public override bool WeaponEquip(String name) 
    {
        return name.Contains("knife") 
    }
}