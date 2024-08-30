using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using JB;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;

public partial class LastRequest
{
    bool CanRebel()
    {
        return Lib.AliveTCount() == 1;
    }

    public void RebelGuns(CCSPlayerController player, ChatMenuOption option)
    {
        if (!player.IsLegal())
            return;

        if (!CanRebel() || rebelType != RebelType.NONE)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.rebel_last");
            return;
        }

        Weapon.GunMenuGive(player,option);
    
        player.SetHealth(JB.Lib.AliveCtCount() * 100);

        rebelType = RebelType.REBEL;

        Chat.LocalizeAnnounce(LR_PREFIX,"lr.rebel_name",player.PlayerName);
    }

    public void StartRebel(CCSPlayerController? player, ChatMenuOption option)
    {
        if (!player.IsLegalAlive())
            return;

        player.GunMenuInternal(false,RebelGuns);
    }

    public void StartKnifeRebel(CCSPlayerController? rebel, ChatMenuOption option)
    {
        if (!rebel.IsLegalAlive())
            return;

        if (!CanRebel())
        {
            rebel.LocalizePrefix(LR_PREFIX,"rebel.last_alive");
            return;
        }

        rebelType = RebelType.KNIFE;

        Chat.LocalizeAnnounce(LR_PREFIX,"lr.knife_rebel",rebel.PlayerName);
        rebel.SetHealth(Lib.AliveCtCount() * 100);

        foreach (CCSPlayerController? player in JB.Lib.GetPlayers())
        {
            if (player.IsLegalAlive())
                player.StripWeapons();
        }
    }

    public void RiotRespawn()
    {
        // riot cancelled in mean time
        if (rebelType != RebelType.RIOT)
            return;

        Chat.LocalizeAnnounce(LR_PREFIX,"lr.riot_active");

        foreach (CCSPlayerController? player in JB.Lib.GetPlayers())
        {
            if (!player.IsLegalAlive() && player.IsT())
                player.Respawn();
        }
    }


    public void StartRiot(CCSPlayerController? rebel, ChatMenuOption option)
    {
        if (!rebel.IsLegal())
            return;

        if (!CanRebel())
        {
            rebel.LocalizePrefix(LR_PREFIX,"lr.rebel_last");
            return;
        }


        rebelType = RebelType.RIOT;

        Chat.LocalizeAnnounce(LR_PREFIX,"lr.riot_start");

        if (JB.JailPlugin.globalCtx != null)
            JB.JailPlugin.globalCtx.AddTimer(15.0f,RiotRespawn,CSTimer.TimerFlags.STOP_ON_MAPCHANGE);
    }

    enum RebelType
    {
        NONE,
        REBEL,
        KNIFE,
        RIOT,
    };

    RebelType rebelType = RebelType.NONE;
}