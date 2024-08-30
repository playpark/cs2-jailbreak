using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class Warday
{
    void gun_callback(int unused)
    {
        // if warday is no longer active dont allow guns

        if (wardayActive)
        {
            if (Config.Settings.WardayGuns)
            {
                // give T guns
                foreach(CCSPlayerController player in JB.Lib.GetAliveT())
                    player.EventGunMenu();
            }

            Entity.ForceOpen();

            Chat.LocalizeAnnounce(WARDAY_PREFIX,"warday.live");
        }
    }

    public bool StartWarday(String location, int delay)
    {
        if (roundCounter >= ROUND_LIMIT)
        {
            // must wait again to start a warday
            roundCounter = 0;

            wardayActive = true;
            JB.JailPlugin.StartEvent();
            
            Entity.ForceClose();

            if (Config.Settings.WardayGuns)
            {
                foreach(CCSPlayerController player in JB.Lib.GetPlayers())
                {
                    if (player.IsLegal() && player.IsCt())
                        player.EventGunMenu();
                }
            }

            countdown.Start(Chat.Localize("warday.location",location),delay,0,null,gun_callback);
            return true;
        }        

        return false;
    }

    public void RoundEnd()
    {
        countdown.Kill();
    }

    public void RoundStart()
    {
        // one less round till a warday can be called
        roundCounter++;

        countdown.Kill();

        wardayActive = false;
        JB.JailPlugin.EndEvent();
    }

    public void MapStart()
    {
        // give a warday on map start
        roundCounter = ROUND_LIMIT;
    }

    public JailConfig Config = new JailConfig();

    public static String WARDAY_PREFIX = $" {ChatColors.Green} [Warday]: {ChatColors.White}";

    bool wardayActive = false;

    public int roundCounter = ROUND_LIMIT;

    public const int ROUND_LIMIT = 3;

    Countdown<int> countdown = new Countdown<int>();
};