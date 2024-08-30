using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

public class Block
{
    [RequiresPermissions("@jail/debug")]
    public void IsBlocked(CCSPlayerController? invoke, CommandInfo command)
    {
        invoke.Announce(Debug.DEBUG_PREFIX,$"Block state {blockState} : {JB.Lib.BlockEnabled()}");
    }

    public void BlockAll()
    {
        if (!JB.Lib.BlockEnabled())
        {
            Chat.LocalizeAnnounce(Warden.WARDEN_PREFIX,"block.enable");
            JB.Lib.BlockAll();
            blockState = true;
        }
    }

    public void UnBlockAll()
    {
        if (JB.Lib.BlockEnabled())
        {
            Chat.LocalizeAnnounce(Warden.WARDEN_PREFIX,"block.disable");
            JB.Lib.UnBlockAll();
            blockState = false;
        }
    }

    public void RoundStart()
    {
        if (Config.Settings.NoBlock)
            UnBlockAll();

        else BlockAll();
    }

    public JailConfig Config = new JailConfig();
 
    public bool blockState = false;
}