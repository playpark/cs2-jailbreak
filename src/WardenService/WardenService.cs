
using CounterStrikeSharp.API.Core;

public interface IWardenService
{
    public bool IsWarden(CCSPlayerController? player);
    public CCSPlayerController? GetWarden();
    public void SetWarden(CCSPlayerController player);
}