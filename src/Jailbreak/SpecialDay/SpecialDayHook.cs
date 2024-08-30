using CounterStrikeSharp.API.Core;

public partial class SpecialDay
{

    public void RoundEnd()
    {
        EndSD();
    }

    public void RoundStart()
    {
        // increment our round counter
        wsdRound += 1;
        EndSD();
    }

    public void WeaponEquip(CCSPlayerController? player,String name) 
    {
        if (!player.IsLegalAlive())
            return;

        if (activeSD != null)
        {
            // weapon equip not valid drop the weapons
            if (!activeSD.WeaponEquip(player,name))
                activeSD.SetupPlayer(player);
        }
    }

    public void Disconnect(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        if (activeSD != null)
            activeSD.Disconnect(player);
    }

    public void GrenadeThrown(CCSPlayerController? player)
    {
        if (activeSD != null)
            activeSD.GrenadeThrown(player);   
    }

    public void EntCreated(CEntityInstance entity)
    {
        if (activeSD != null)
            activeSD.EntCreated(entity);
    }

    public void Death(CCSPlayerController? player, CCSPlayerController? attacker, String weapon)
    {
        if (activeSD != null)
            activeSD.Death(player,attacker, weapon);
    }

    public void PlayerHurt(CCSPlayerController? player, CCSPlayerController? attacker, int damage,int health, int hitgroup)
    {
        if (activeSD != null && player.IsLegal())
            activeSD.PlayerHurt(player,attacker,damage,health,hitgroup);
    }

    public void TakeDamage(CCSPlayerController? player, CCSPlayerController? attacker, ref float damage)
    {
        if (activeSD == null || !player.IsLegal())
            return;

        if(activeSD.restrictDamage)
            damage = 0.0f;
    }
}