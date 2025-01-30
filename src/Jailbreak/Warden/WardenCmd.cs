
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using JB;
using CounterStrikeSharp.API.Modules.Menu;

public partial class Warden
{
    public void LeaveWardenCmd(CCSPlayerController? player)
    {
        RemoveIfWarden(player);
    }

    public void RemoveMarkerCmd(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        if (IsWarden(player))
        {
            player.Announce(WARDEN_PREFIX, "Marker removed");
            RemoveMarker();
        }
    }

    [RequiresPermissions("@css/generic")]
    public void RemoveWardenCmd(CCSPlayerController? player)
    {
        Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.remove");
        RemoveWarden();
    }

    private bool openedCells = false;
    public void ForceDoorsCmd(CCSPlayerController? invoke)
    {
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, "you must be warden to use this command");
            return;
        }

        if (!openedCells)
            Entity.ForceOpen();

        else if (openedCells)
            Entity.ForceClose();

        openedCells = openedCells ? false : true;
    }

    public void WardayCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (!player.IsLegal())
            return;

        // must be warden
        if (!IsWarden(player))
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.warday_restrict");
            return;
        }

        // must specify location
        if (command.ArgCount < 2)
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.warday_usage");
            return;
        }

        // attempt the start the warday
        String location = command.ArgByIndex(1);

        // attempt to parse optional delay
        int delay = 20;

        if (command.ArgCount >= 3)
        {
            if (Int32.TryParse(command.ArgByIndex(2),out int delayOpt))
            {
                delay = delayOpt;

                if (delayOpt > 200)
                {
                    player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_max_delay");
                    return;
                }
            }       
        }

        if (!warday.StartWarday(location,delay))
            player.LocalizePrefix(WARDEN_PREFIX, "warden.warday_round_restrict", Warday.ROUND_LIMIT - warday.roundCounter);
    }


    public void CountdownCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (!player.IsLegal())
            return;

        // must be warden
        if (!IsWarden(player))
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_restrict");
            return;
        }

        // must specify location
        if (command.ArgCount < 2)
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_usage");
            return;
        }

        // attempt the start the warday
        String str = command.ArgByIndex(2);

        // attempt to parse optional delay
        int delay = 20;

        if (command.ArgCount >= 3)
        {
            if (Int32.TryParse(command.ArgByIndex(1),out int delayOpt))
            {
                delay = delayOpt;

                // Thanks TICHO
                if (delayOpt > 200)
                {
                    player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_max_delay");
                    return;
                }
            }   

            else
            {
                player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_usage");
                return;      
            }    
        }

        chatCountdown.Start(str,delay,0,null,null);
    }

    public void CountdownAbortCmd(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        // must be warden
        if (!IsWarden(player))
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.countdown_restrict");
            return;
        }

        Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.countdown_abort");

        chatCountdown.Kill();
    }

    (JailPlayer?, CCSPlayerController?) GiveTInternal(CCSPlayerController? invoke, String name, String playerName)
    {
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, $"You must be the warden to give a {name}");
            return (null,null);
        }

        int slot = Player.SlotFromName(playerName);

        if (slot != -1)
        {
            JailPlayer jailPlayer = jailPlayers[slot];
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);

            return (jailPlayer,player);
        }

        return (null,null);
    }

    public void GiveFreedayCallback(CCSPlayerController? invoke, ChatMenuOption option)
    {
        var (jailPlayer,player) = GiveTInternal(invoke,"freeday",option.Text!);

        jailPlayer?.GiveFreeday(player);  
    }

    public void GivePardonCallback(CCSPlayerController? invoke, ChatMenuOption option)
    {
        var (jailPlayer,player) = GiveTInternal(invoke,"pardon",option.Text!);

        jailPlayer?.GivePardon(player);  
    }

    public bool IsAliveRebel(CCSPlayerController? player)
    {
        var jailPlayer = JailPlayerFromPlayer(player);

        if (jailPlayer != null)
            return jailPlayer.isRebel(player) && player.IsLegalAlive();

        return false;
    }

    public void WardenMuteCmd(CCSPlayerController? invoke)
    {
        // make sure we are actually the warden
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, "you must be warden to use a warden mute");
            return;
        }

        if (tmpMuteTimer != null)
        {
            invoke.Announce(WARDEN_PREFIX, "mute is already active");
            return;
        }

        long remain = 60 - (Lib.CurTimestamp() - tmpMuteTimestamp);

        // make sure we cant spam this
        if (remain > 0)
        {
            invoke.Announce(WARDEN_PREFIX, $"Warden mute cannot be used for another {remain} seconds");
            return;
        }

        // mute everyone that isnt the warden
        foreach(CCSPlayerController player in Lib.GetAlivePlayers())
        {
            if (!IsWarden(player))
                player.Mute();
        }

        Chat.Announce(WARDEN_PREFIX, "everyone apart from the warden is muted for 10 seconds!");

        tmpMuteTimer = JailPlugin.globalCtx.AddTimer(10.0f, UnmuteTmp, CSTimer.TimerFlags.STOP_ON_MAPCHANGE);
    }

    public void HealTCmd(CCSPlayerController? invoke)
    {
        // make sure we are actually the warden
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, "you must be warden to heal t's");
            return;
        }

        Chat.Announce(WARDEN_PREFIX, "Warden healed t's");

        foreach(CCSPlayerController player in Lib.GetAliveT())
            player.SetHealth(100); 
    }

    public void UnmuteTmp()
    {
        Chat.Announce(WARDEN_PREFIX, "warden mute is now over!");

        Lib.UnMuteAll();
        tmpMuteTimer = null;

        // re grab the timestmap for the cooldown
        tmpMuteTimestamp = Lib.CurTimestamp();
    }

    public void GiveT(CCSPlayerController? invoke, String name, Action<CCSPlayerController, ChatMenuOption> callback,Func<CCSPlayerController?,bool> filter)
    {
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, $"Must be warden to give {name}");
            return;
        }

        JB.Lib.InvokePlayerMenu(invoke,name,callback,filter);
    }

    public void ColourCallback(CCSPlayerController? invoke, ChatMenuOption option)
    {
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, $"You must be the warden to colour t's");
            return;        
        }

        CCSPlayerController? player = Utilities.GetPlayerFromSlot(colourSlot);

        if (player.IsLegalAlive())
        {
            Color colour = JB.Lib.COLOUR_CONFIG_MAP[option.Text!];

            Chat.Announce(WARDEN_PREFIX, $"Setting {player.PlayerName} colour to {option.Text!}");
            player.SetColour(colour);
        }
    }

    public void ColourPlayerCallback(CCSPlayerController? invoke, ChatMenuOption option)
    {
        // save this slot for 2nd stage of the command
        colourSlot = Player.SlotFromName(option.Text!);

        CCSPlayerController? player = Utilities.GetPlayerFromSlot(colourSlot);

        if (player.IsLegalAlive())
            Lib.ColourMenu(invoke,ColourCallback,$"Player colour {player.PlayerName}");

        else invoke.Announce(WARDEN_PREFIX, $"No such alive player {option.Text} to colour");
    }

    public void ColourCmd(CCSPlayerController? invoke)
    {
        if (!IsWarden(invoke))
        {
            invoke.Announce(WARDEN_PREFIX, $"You must be the warden to colour t's");
            return;
        }

        JB.Lib.InvokePlayerMenu(invoke,"Colour",ColourPlayerCallback,Player.IsLegalAliveT);
    }

    public void GiveFreedayCmd(CCSPlayerController? invoke)
    {
        GiveT(invoke,"Freeday",GiveFreedayCallback,Player.IsLegalAliveT);
    }

    public void GivePardonCmd(CCSPlayerController? invoke)
    {
        GiveT(invoke,"Pardon",GivePardonCallback,IsAliveRebel);
    }

    public void WNoBlockCmd(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        // must be warden
        if (!IsWarden(player))
        {
            player.LocalizePrefix(WARDEN_PREFIX, "warden.wub_restrict");
            return;
        }

        block.blockState = !block.blockState;

        if (block.blockState)
            block.UnBlockAll();

        else block.BlockAll();
    }

    // debug command
    [RequiresPermissions("@jail/debug")]
    public void IsRebelCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        if (!invoke.IsLegal())
            return;

        invoke.PrintToConsole("rebels\n");

        foreach(CCSPlayerController player in JB.Lib.GetPlayers())
            invoke.PrintToConsole($"{jailPlayers[player.Slot].isRebel} : {player.PlayerName}\n");
    }

    public void WardenTimeCmd(CCSPlayerController? invoke)
    {
        if (!invoke.IsLegal())
            return;

        if(wardenSlot == INAVLID_SLOT)
        {
            invoke.LocalizePrefix(WARDEN_PREFIX, "warden.no_warden");
            return;
        }

        long elaspedMin = (JB.Lib.CurTimestamp() - wardenTimestamp) / 60;

        invoke.LocalizePrefix(WARDEN_PREFIX, "warden.time", elaspedMin);
    }

    public void CmdInfo(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        string WardenCommandsPath = Path.Combine(JB.JailPlugin.globalCtx.ModuleDirectory, "wardencommands.txt");

        if (File.Exists(WardenCommandsPath))
        {
            foreach (string line in File.ReadLines(WardenCommandsPath))
                player.PrintPrefix(WARDEN_PREFIX, line);
        }
    }

    public void TakeWardenCmd(CCSPlayerController? player)
    {
        // invalid player we dont care
        if (!player.IsLegal())
            return;

        // player must be alive
        if (!player.IsLegalAlive())
            player.LocalizePrefix(WARDEN_PREFIX, "warden.warden_req_alive");   

        // check team is valid
        else if (!player.IsCt())
            player.LocalizePrefix(WARDEN_PREFIX, "warden.warden_req_ct");

        // check there is no warden
        else if (wardenSlot != INAVLID_SLOT)
        {
            var warden = Utilities.GetPlayerFromSlot(wardenSlot);

            if (warden.IsLegal())
                player.LocalizePrefix(WARDEN_PREFIX,"warden.warden_taken",warden.PlayerName);
        }

        // player is valid to take warden
        else SetWarden(player.Slot);
    }

    [RequiresPermissions("@css/generic")]
    public void FireGuardCmd(CCSPlayerController? invoke)
    {
        Chat.LocalizeAnnounce(WARDEN_PREFIX, "warden.fire_guard");

        // swap every guard apart from warden to T
        List<CCSPlayerController> players = JB.Lib.GetPlayers();
        var valid = players.FindAll(player => player.IsCt() && !IsWarden(player));

        foreach(var player in valid)
        {
            player.Slay();
            player.SwitchTeam(CsTeam.Terrorist);
        }
    }

    public void CtGuns(CCSPlayerController player, ChatMenuOption option)
    {
        if (!player.IsLegalAlive() || !player.IsCt()) 
            return;

        player.StripWeapons();

        var jailPlayer = JailPlayerFromPlayer(player);

        if (jailPlayer != null)
        {
            jailPlayer.UpdatePlayer(player, "ct_gun", option.Text!);
            jailPlayer.ctGun = option.Text!;
        }

        player.GiveMenuWeapon(option.Text!);
        player.GiveWeapon("deagle");

        if (Config.Guard.Armor)
            player.GiveArmor();
    }

    public void CmdCtGuns(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        if(!player.IsCt())
        {
            player.LocalizeAnnounce(WARDEN_PREFIX, "warden.ct_gun_menu");
            return;
        }

        if(!Config.Guard.GunMenu)
        {
            player.LocalizeAnnounce(WARDEN_PREFIX, "warden.gun_menu_disabled");
            return;
        }

        player.GunMenuInternal(true,CtGuns);     
    }
}