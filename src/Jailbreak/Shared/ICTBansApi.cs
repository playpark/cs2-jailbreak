using CounterStrikeSharp.API.Core;

namespace CTBans.Shared
{
    /// <summary>
    /// Interface for the CTBans API that can be used by other plugins
    /// </summary>
    public interface ICTBansApi
    {
        /// <summary>
        /// Checks if a player has an active CT ban
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>True if the player is banned, false otherwise</returns>
        bool IsPlayerCTBanned(CCSPlayerController? player);

        /// <summary>
        /// Gets the remaining time of a player's CT ban
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>The remaining time as a formatted string, or null if not banned</returns>
        string? GetPlayerCTBanTimeRemaining(CCSPlayerController? player);

        /// <summary>
        /// Gets the reason for a player's CT ban
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>The reason for the ban, or null if not banned</returns>
        string? GetPlayerCTBanReason(CCSPlayerController? player);

        /// <summary>
        /// Checks if a player has an active CT ban and sends them a message if they do
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>True if the player is banned, false otherwise</returns>
        bool CheckAndNotifyPlayerCTBan(CCSPlayerController? player);
    }
}