
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Core.Capabilities;
using CSTimer = CounterStrikeSharp.API.Modules.Timers;

namespace JB;

public class WardenService : IWardenService
{
    public bool IsWarden(CCSPlayerController? player)
    {
        return JailPlugin.IsWarden(player);
    }

    public void SetWarden(CCSPlayerController player)
    {
        if (player.IsLegalAlive() && player.IsCt())
            JailPlugin.warden.SetWarden(player.Slot);
    }

    public CCSPlayerController? GetWarden()
    {
        return JailPlugin.warden.GetWarden();
    }
}
 
// main plugin file, controls central hooking
// defers to warden, lr and sd
[MinimumApiVersion(244)]
public class JailPlugin : BasePlugin, IPluginConfig<JailConfig>
{
    public override string ModuleName => "Jailbreak";
    public override string ModuleVersion => "0.5.0";
    public override string ModuleAuthor => "destoer, continued by exkludera";
    
    // Global event settings, used to filter plugin activits
    // during warday and SD
    bool isEventActive = false;

    public JailConfig Config  { get; set; } = new JailConfig();

    public static PluginCapability<IWardenService> wardenService {get; } = new ("jailbreak:warden_service");

    public static bool IsWarden(CCSPlayerController? player)
    {
        return warden.IsWarden(player);
    }

    public static bool EventActive()
    {
        return globalCtx.isEventActive;
    }

    public static void StartEvent()
    {
        globalCtx.isEventActive = true;
    }

    public static void EndEvent()
    {
        globalCtx.isEventActive = false;
    }

    public static void WinLR(CCSPlayerController? player,LastRequest.LRType type)
    {
        jailStats.Win(player,type);
    }

    public static void LoseLR(CCSPlayerController? player, LastRequest.LRType type)
    {
        jailStats.Loss(player,type);
    }

    public static void PurgePlayerStats(CCSPlayerController? player)
    {
        jailStats.PurgePlayer(player);
    }

    public override void Load(bool hotReload)
    {
        globalCtx = this;
        logs = new Logs(this); 

        Capabilities.RegisterPluginCapability(wardenService,() => new WardenService());

        RegisterCommands();
        
        RegisterHooks();

        RegisterListeners();

        LocalizePrefix();

        JailPlayer.SetupDB();

        Console.WriteLine("Sucessfully started JB");

        AddTimer(Warden.LASER_TIME,warden.LaserTick,CSTimer.TimerFlags.REPEAT);
    }

    void LocalizePrefix()
    {
        LastRequest.LR_PREFIX = Chat.Localize("lr.lr_prefix");
        Entity.DOOR_PREFIX = Chat.Localize("warden.door_prefix");

        SpecialDay.SPECIALDAY_PREFIX = Chat.Localize("sd.sd_prefix");
        JailPlayer.REBEL_PREFIX = Chat.Localize("rebel.rebel_prefix");

        Mute.MUTE_PREFIX = Chat.Localize("mute.mute_prefix");
        Warden.TEAM_PREFIX = Chat.Localize("warden.team_prefix");
        
        Warday.WARDAY_PREFIX = Chat.Localize("warday.warday_prefix");
        Warden.WARDEN_PREFIX = Chat.Localize("warden.warden_prefix");    
    }

    void StatDBReload()
    {
        Task.Run(async () => 
        {
            var database = await jailStats.ConnectDB();

            jailStats.SetupDB(database);
        });
    }

    public void OnConfigParsed(JailConfig config)
    {
        // give each sub plugin the config
        this.Config = config;
        
        jailStats.Config = config;
        lr.Config = config;

        warden.Config = config;
        warden.mute.Config = config;
        warden.warday.Config = config;
        warden.block.Config = config;
        JailPlayer.Config = config;

        sd.Config = config;

        lr.LRConfigReload();
        StatDBReload();
    }

    void RegisterListeners()
    {
        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            lr.EntCreated(entity);
            sd.EntCreated(entity);
        });
    }

    public static void KillCmd(CCSPlayerController? invoke)
    {  
        if (invoke.IsLegalAlive())
        {
            Chat.LocalizeAnnounce("","jail.kill_cmd",invoke.PlayerName);
            invoke.Slay();
        }
    }

    void AddCmd(string commands, string desc, Action<CCSPlayerController?> callback)
    {
        foreach (var command in commands.Split(','))
            AddCommand($"css_{command}", desc, (player, command) => callback(player));
    }

    void RegisterCommands()
    {
        //other
        AddCmd(Config.Settings.Commands.Kill, "kill", KillCmd);
        AddCmd(Config.Settings.Commands.WardenTime, "how long as warden been active?", warden.WardenTimeCmd);

        AddCmd(Config.Guard.Commands.Guns, "gun menu", warden.CmdCtGuns);

        //warden
        AddCmd(Config.Guard.Warden.Commands.Warden, "take warden", warden.TakeWardenCmd);
        AddCmd(Config.Guard.Warden.Commands.UnWarden, "leave warden", warden.LeaveWardenCmd);

        AddCmd(Config.Guard.Warden.Commands.RemoveMarker, "remove warden marker", warden.RemoveMarkerCmd);
        AddCmd(Config.Guard.Warden.Commands.MarkerColor, "set marker color", warden.MarkerColourCmd);
        AddCmd(Config.Guard.Warden.Commands.LaserColor, "set laser color", warden.LaserColourCmd);
        AddCmd(Config.Guard.Warden.Commands.Color, "set player color", warden.ColourCmd);

        AddCmd(Config.Guard.Warden.Commands.NoBlock, "toggle noblock", warden.WNoBlockCmd);
        AddCmd(Config.Guard.Warden.Commands.HealPrisoners, "Heal t's", warden.HealTCmd);

        AddCmd(Config.Guard.Warden.Commands.SpecialDay, "warden : call a special day", sd.WardenSDCmd);
        AddCmd(Config.Guard.Warden.Commands.SpecialDayFF, "warden : call a friendly fire special day", sd.WardenSDFFCmd);


        AddCommand(Config.Guard.Warden.Commands.Warday, "warden : start warday", warden.WardayCmd);

        AddCmd(Config.Guard.Warden.Commands.ListCommands, "warden : show all commands", warden.CmdInfo);

        AddCmd(Config.Guard.Warden.Commands.GiveFreeday, "give t a freeday", warden.GiveFreedayCmd);

        AddCmd(Config.Guard.Warden.Commands.GivePardon, "give t a pardon", warden.GivePardonCmd);

        AddCmd(Config.Guard.Warden.Commands.Countdown, "start a countdown", warden.CountdownAbortCmd);

        AddCommand(Config.Guard.Warden.Commands.CountdownAbort, "abort a countdown", warden.CountdownCmd);

        AddCmd(Config.Guard.Warden.Commands.Mute, "do a warden mute", warden.WardenMuteCmd);

        //last request
        AddCmd(Config.Prisoner.LR.Commands.Start, "start lr", lr.LRCmd);
        AddCmd(Config.Prisoner.LR.Commands.Cancel, "cancel lr", lr.CancelLRCmd);
        AddCmd(Config.Prisoner.LR.Commands.Stats, "list lr stats", jailStats.LRStatsCmd);

        //admin
        AddCmd(Config.Settings.AdminCommands.Logs, "show round logs", logs.LogsCommand);
        AddCmd(Config.Settings.AdminCommands.RemoveWarden, "remove warden", warden.RemoveWardenCmd);
        AddCmd(Config.Settings.AdminCommands.SpecialDay, "start a sd", sd.SDCmd);
        AddCmd(Config.Settings.AdminCommands.SpecialDayFF, "start a ff sd", sd.SDFFCmd);
        AddCmd(Config.Settings.AdminCommands.SpecialDayCancel, "cancel an sd", sd.CancelSDCmd);
        AddCmd(Config.Settings.AdminCommands.FireGuard, "admin : Remove all guards apart from warden", warden.FireGuardCmd);
        AddCommand(Config.Settings.AdminCommands.SwapGuard, "admin : move a player to ct", warden.SwapGuardCmd);

        AddCmd(Config.Settings.AdminCommands.ForceDoors, "force open/close every door", warden.ForceDoorsCmd);
        
        // debug 
        if (Debug.enable)
        {
            AddCommand("nuke","debug : kill every player",Debug.Nuke);
            AddCommand("is_rebel","debug : print rebel state to console",warden.IsRebelCmd);
            AddCommand("lr_debug","debug : start an lr without restriction",lr.LRDebugCmd);
            AddCommand("is_blocked","debug : print block state",warden.block.IsBlocked);
            AddCommand("test_laser","test laser",Debug.TestLaser);
            AddCommand("test_player","testt player",Debug.TestPlayer);
            AddCommand("test_strip","test weapon strip",Debug.TestStripCmd);
            AddCommand("join_ct_debug","debug : force join ct",Debug.JoinCtCmd);
            AddCommand("hide_weapon_debug","debug : hide player weapon on back",Debug.HideWeaponCmd);
            AddCommand("rig","debug : force player to boss on sd",sd.SDRigCmd);
            AddCommand("is_muted","debug : print voice flags",Debug.IsMutedCmd);
            AddCommand("spam_db","debug : spam db",Debug.TestLRInc);
            AddCommand("wsd_enable","debug : enable wsd",Debug.WSDEnableCmd);
            AddCommand("test_noblock","debug : enable wsd",Debug.TestNoblockCmd);
        }
    }

    public HookResult JoinTeam(CCSPlayerController? invoke, CommandInfo command)
    {
        jailStats.LoadPlayer(invoke);

        JailPlayer? jailPlayer = warden.JailPlayerFromPlayer(invoke);

        if (jailPlayer != null)
            jailPlayer.LoadPlayer(invoke);  

        if (!warden.JoinTeam(invoke,command))
            return HookResult.Handled;

        return HookResult.Continue;
    }

    void RegisterHooks()
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd,HookMode.Pre);
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventTeamchangePending>(OnSwitchTeam);
        RegisterEventHandler<EventMapTransition>(OnMapChange);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath,HookMode.Pre);
        RegisterEventHandler<EventItemEquip>(OnItemEquip);
        RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterEventHandler<EventWeaponZoom>(OnWeaponZoom);
        RegisterEventHandler<EventPlayerPing>(OnPlayerPing);

        // take damage causes crashes on windows
        // cant figure out why because the windows cs2 console wont log
        // before it dies
        if (!Lib.IsWindows())
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage,HookMode.Pre);
        
        HookEntityOutput("func_button", "OnPressed", OnButtonPressed);
        
        RegisterListener<Listeners.OnClientVoice>(OnClientVoice);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);

        AddCommandListener("jointeam",JoinTeam);
        AddCommandListener("player_ping",PlayerPingCmd);

        AddCommandListener("say", OnPlayerChat);

        // TODO: need to hook weapon drop
    }


    public HookResult OnPlayerChat(CCSPlayerController? invoke, CommandInfo command)
    {
        // dont print chat, warden is handling it
        if (!warden.PlayerChat(invoke,command))
            return HookResult.Handled;

        return HookResult.Continue;
    }

    public HookResult PlayerPingCmd(CCSPlayerController? invoke, CommandInfo command)
    {
        // if player is not warden ignore the ping
        if (Config.Settings.RestrictPing && !warden.IsWarden(invoke))
            return HookResult.Handled;

        return HookResult.Continue;
    }

    HookResult OnPlayerPing(EventPlayerPing  @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player.IsLegal())
            warden.Ping(player,@event.X,@event.Y,@event.Z);

        return HookResult.Continue;
    }

    void OnClientVoice(int slot)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);

        if (player.IsLegal())
            warden.Voice(player);
    }

    // button log
    HookResult OnButtonPressed(CEntityIOOutput output, String name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        CCSPlayerController? player = new CBaseEntity(activator.Handle).Player();

        // grab player controller from pawn
        CBaseEntity? ent =  Utilities.GetEntityFromIndex<CBaseEntity>((int)caller.Index);

        if (player.IsLegal() && ent != null && ent.IsValid)
            logs.AddLocalized(player, "logs.format.button", ent.Entity?.Name ?? "Unlabeled", output?.Connections?.TargetDesc ?? "None");

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage,HookMode.Pre);
    }

    HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player.IsLegal())
        {
            lr.GrenadeThrown(player);
            sd.GrenadeThrown(player);
            logs.AddLocalized(player, "logs.format.grenade", @event.Weapon); 
        }

        return HookResult.Continue;
    }
  
    HookResult OnWeaponZoom(EventWeaponZoom @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player.IsLegal())
            lr.WeaponZoom(player);

        return HookResult.Continue;
    }

    HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player.IsLegal())
        {
            lr.WeaponEquip(player,@event.Item);
            sd.WeaponEquip(player,@event.Item);
        }

        return HookResult.Continue;
    }

    HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        int damage = @event.DmgHealth;
        int health = @event.Health;
        int hitgroup = @event.Hitgroup;

        if (player.IsLegal())
        {
            lr.PlayerHurt(player,attacker,damage,health,hitgroup);
            warden.PlayerHurt(player,attacker,damage,health);
            sd.PlayerHurt(player,attacker,damage,health,hitgroup);
        }

        return HookResult.Continue;
    }

    HookResult OnTakeDamage(DynamicHook handle)
    {
        CEntityInstance victim = handle.GetParam<CEntityInstance>(0);
        CTakeDamageInfo damage_info = handle.GetParam<CTakeDamageInfo>(1);

        CHandle<CBaseEntity> dealer = damage_info.Attacker;

        // get player and attacker
        CCSPlayerController? player = new CBaseEntity(victim.Handle).Player();
        CCSPlayerController? attacker = dealer.Player();

        if (player.IsLegal())
        {
            warden.TakeDamage(player,attacker,ref damage_info.Damage);
            sd.TakeDamage(player,attacker,ref damage_info.Damage);
            lr.TakeDamage(player,attacker,ref damage_info.Damage);
        }
        
        return HookResult.Continue;
    }

    HookResult OnMapChange(EventMapTransition @event, GameEventInfo info)
    {
        warden.MapStart();

        return HookResult.Continue;
    }

    HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        warden.RoundStart();
        lr.RoundStart();
        sd.RoundStart();

        return HookResult.Continue;
    }

    HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? killer = @event.Attacker;

        // NOTE: have to check IsConnected incase this is tripped by a dc
    
        // hide t killing ct
        if (Config.Settings.HideKills && victim.IsConnected() && killer.IsConnected() && killer.IsT() && victim.IsCt())
        {
            killer.Announce(Warden.WARDEN_PREFIX,$"You killed: {victim.PlayerName}");
            info.DontBroadcast = true;
        }
    
        if (victim.IsLegal() && victim.IsConnected())
        {
            warden.Death(victim,killer);
            lr.Death(victim);
            sd.Death(victim,killer,@event.Weapon);
        }
        return HookResult.Continue;
    }

    HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player.IsLegal())
        {
            int slot = player.Slot;

            AddTimer(0.5f,() =>  
            {
                warden.Spawn(Utilities.GetPlayerFromSlot(slot));
            });
            
        }

        return HookResult.Continue;
    }

    HookResult OnSwitchTeam(EventTeamchangePending @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        int new_team = @event.Toteam;

        if (player.IsLegal())
        {
            // close menu on team switch to prevent illegal usage
            //MenuManager.CloseActiveMenu(player);
            warden.SwitchTeam(player,new_team);
        }

        return HookResult.Continue;
    }

    public void OnClientAuthorized(int slot, SteamID steamid)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);

        if (player.IsLegal())
        {
            // load in player stats
            jailStats.LoadPlayer(player);
            
            JailPlayer? jailPlayer = warden.JailPlayerFromPlayer(player);

            if (jailPlayer != null)
                jailPlayer.LoadPlayer(player);
        }
    }

    HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player.IsLegal())
        {
            warden.Disconnect(player);
            lr.Disconnect(player);
            sd.Disconnect(player);
        }

        return HookResult.Continue;
    }

    HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        warden.RoundEnd();
        lr.RoundEnd();
        sd.RoundEnd();

        return HookResult.Continue;
    }

    HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        // attempt to get player and weapon
        var player = @event.Userid;
        String name = @event.Weapon;

        if (player.IsLegalAlive())
        {
            warden.WeaponFire(player,name);
            lr.WeaponFire(player,name);
        }

        return HookResult.Continue;
    }

    public static String Localize(string name,params Object[] args)
    {
        return String.Format(globalCtx.Localizer[name],args);
    }

    public static Warden warden = new Warden();
    public static LastRequest lr = new LastRequest();
    public static SpecialDay sd = new SpecialDay();
    public static JailStats jailStats = new JailStats();
    //public static PlayerInfo playerInfo = new PlayerInfo();

    // in practice these wont be null
#pragma warning disable CS8618
    public static Logs logs;

    // workaround to query global state!
    public static JailPlugin globalCtx;

    #pragma warning restore CS8618
}