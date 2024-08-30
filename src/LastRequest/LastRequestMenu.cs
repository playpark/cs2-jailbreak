using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using JB;

public partial class LastRequest
{
    bool CanStartLR(CCSPlayerController? player)
    {
        if(!player.IsLegal())
            return false;

        // prevent starts are round begin to stop lr activations on map joins
        if (Lib.CurTimestamp() - startTimestamp < 15)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.wait");
            return false;
        }

        if (!IsValidT(player))
        {
            return false;
        } 

        if (JB.JailPlugin.warden.IsAliveRebel(player) && !Config.Prisoner.LR.AllowRebel)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.rebel_cant_lr");
            return false;
        }

        if (Lib.AliveTCount() > activeLR.Length)
        {
            player.LocalizePrefix(LR_PREFIX,"lr.too_many",activeLR.Length);
            return false;
        }

        return true;
    }

    public void FinaliseChoice(CCSPlayerController? player, ChatMenuOption option)
    {
        // called from pick_parter -> finalise the type struct
        LRChoice? choice = ChoiceFromPlayer(player);

        if (choice == null)
            return;

        string name = option.Text!;

        choice.ctSlot = Player.SlotFromName(name);

        // finally setup the lr
        InitLR(choice);
    }

    public void PickedOption(CCSPlayerController? player, ChatMenuOption option)
    {
        PickPartnerInternal(player,option.Text!);
    }

    public void PickOption(CCSPlayerController? player, ChatMenuOption option)
    {
        // called from lr_type selection
        // save type
        LRChoice? choice = ChoiceFromPlayer(player);

        if (choice == null || !player.IsLegal())
            return;

        choice.type = TypeFromName(option.Text!);

        String lrName = LR_NAME[(int)choice.type];

        // now select option
        switch (choice.type)
        {
            case LRType.KNIFE:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("Vanilla", PickedOption);
                lrMenu.AddMenuOption("Low gravity", PickedOption);
                lrMenu.AddMenuOption("High speed", PickedOption);
                lrMenu.AddMenuOption("One hit", PickedOption);
                
                MenuManager.OpenChatMenu(player, lrMenu);                
                break;
            }

            case LRType.DODGEBALL:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("Vanilla", PickedOption);
                lrMenu.AddMenuOption("Low gravity", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.WAR:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("XM1014", PickedOption);
                lrMenu.AddMenuOption("M249", PickedOption);
                lrMenu.AddMenuOption("P90", PickedOption);
                lrMenu.AddMenuOption("AK47", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.NO_SCOPE:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("Awp", PickedOption);
                lrMenu.AddMenuOption("Scout", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;                
            }

            case LRType.GRENADE:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("Vanilla", PickedOption);
                lrMenu.AddMenuOption("Low gravity", PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            case LRType.SHOT_FOR_SHOT:
            case LRType.MAG_FOR_MAG:
            {
                ChatMenu lrMenu = new($"Choice Menu ({lrName})");

                lrMenu.AddMenuOption("Deagle",PickedOption);
                //lrMenu.Add("Usp",PickedOption);
                lrMenu.AddMenuOption("Glock",PickedOption);
                lrMenu.AddMenuOption("Five seven",PickedOption);
                lrMenu.AddMenuOption("Dual Elite",PickedOption);

                MenuManager.OpenChatMenu(player, lrMenu);
                break;
            }

            // no choices just pick a partner
            default:
            {
                PickPartnerInternal(player,"");
                break;
            }
        }
    }

    bool LegalLrPartnerT(CCSPlayerController? player)
    {
        return player.IsLegalAliveT() && !InLR(player);
    }

    bool LegalLrPartnerCT(CCSPlayerController? player)
    {
        return player.IsLegalAliveCT() && !InLR(player);
    }

    void PickPartnerInternal(CCSPlayerController? player, String name)
    {
        // called from pick_choice -> pick partner
        LRChoice? choice = ChoiceFromPlayer(player);

        if (choice == null || !player.IsLegal())
            return;

        choice.option = name;

        String lrName = LR_NAME[(int)choice.type];
        String menuName = $"Partner Menu ({lrName})";

        // Debugging pick t's
        if (choice.bypass && player.IsCt())
            JB.Lib.InvokePlayerMenu(player,menuName,FinaliseChoice,LegalLrPartnerT);

        else JB.Lib.InvokePlayerMenu(player,menuName,FinaliseChoice,LegalLrPartnerCT);
    }
}