using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using Microsoft.Data.Sqlite;

public class JailPlayer
{
    public static void SetupDB()
    {
        try
        {
            using (var connection = new SqliteConnection("Data Source=destoer_config.sqlite"))
            {
                connection.Open();

                // create the db
                var create = connection.CreateCommand();
                create.CommandText = $"CREATE TABLE IF NOT EXISTS config (steam_id varchar(64) NOT NULL PRIMARY KEY)";
                create.ExecuteNonQuery();

                String[] colCmd =
                {
                    $"ALTER TABLE config ADD COLUMN laser_colour varchar(64) DEFAULT 'Cyan'",
                    $"ALTER TABLE config ADD COLUMN marker_colour varchar(64) DEFAULT 'Cyan'",
                    $"ALTER TABLE config ADD COLUMN ct_gun varchar(64) DEFAULT 'M4'",
                };

                // start populating our fields
                foreach (var cmd in colCmd)
                {
                    var col = connection.CreateCommand();
                    col.CommandText = cmd;

                    // this may fail on duplicate table entry
                    // we don't really care it does
                    try
                    {
                        col.ExecuteNonQuery();
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    async Task UpdatePlayerDB(String steamID, String name, String value)
    {
        try
        {
            using (var connection = new SqliteConnection("Data Source=destoer_config.sqlite"))
            {
                await connection.OpenAsync();

                // modify one of the setting fields
                using var update = connection.CreateCommand();
                update.CommandText = $"UPDATE config SET {name} = '{value}' WHERE steam_id = @steam_id";
                update.Parameters.AddWithValue("@steam_id", steamID);

                await update.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    async Task InsertPlayerDB(String steamID)
    {
        using (var connection = new SqliteConnection("Data Source=destoer_config.sqlite"))
        {
            try
            {
                await connection.OpenAsync();

                // add a new steam id
                using var insertPlayer = connection.CreateCommand();
                insertPlayer.CommandText = $"INSERT OR IGNORE INTO config (steam_id) VALUES (@steam_id)";
                insertPlayer.Parameters.AddWithValue("@steam_id", steamID);

                await insertPlayer.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    async Task LoadPlayerDB(String steamID)
    {
        using (var connection = new SqliteConnection("Data Source=destoer_config.sqlite"))
        {
            try
            {
                await connection.OpenAsync();

                using var querySteamID = connection.CreateCommand();

                // query steamid
                querySteamID.CommandText = $"SELECT * FROM config WHERE steam_id = @steam_id";
                querySteamID.Parameters.AddWithValue("@steam_id", steamID);

                using var reader = await querySteamID.ExecuteReaderAsync();

                if (reader.Read())
                {
                    // just override this
                    laserColour = JB.Lib.COLOUR_CONFIG_MAP[(String)reader["laser_colour"]];
                    markerColour = JB.Lib.COLOUR_CONFIG_MAP[(String)reader["marker_colour"]];
                    ctGun = (String)reader["ct_gun"];

                    // don't try reloading the player
                    cached = true;
                }
                else
                {
                    await InsertPlayerDB(steamID);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public void LoadPlayer(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        if (cached)
            return;

        String steamID = new SteamID(player.SteamID).SteamId2;

        // make sure this doesn't block the main thread
        Task.Run(async () =>
        {
            await LoadPlayerDB(steamID);
        });
    }

    public void UpdatePlayer(CCSPlayerController? player, String name, String value)
    {
        if (!player.IsLegal())
            return;

        String steamID = new SteamID(player.SteamID).SteamId2;

        // make sure this doesn't block the main thread
        Task.Run(async () =>
        {
            await UpdatePlayerDB(steamID, name, value);
        });
    }

    public void SetLaser(CCSPlayerController? player, String value)
    {
        if (!player.IsLegal())
            return;

        player.Announce(Warden.WARDEN_PREFIX, $"Laser colour set to {value}");
        laserColour = JB.Lib.COLOUR_CONFIG_MAP[value];

        // save back to the db too
        UpdatePlayer(player, "laser_colour", value);
    }

    public void SetMarker(CCSPlayerController? player, String value)
    {
        if (!player.IsLegal())
            return;

        player.Announce(Warden.WARDEN_PREFIX, $"Marker colour set to {value}");
        markerColour = JB.Lib.COLOUR_CONFIG_MAP[value];

        // save back to the db too
        UpdatePlayer(player, "marker_colour", value);
    }

    public void PurgeRound()
    {
        RebelList.Clear();
        playerRebel.Clear();
    }

    public void Reset()
    {
        PurgeRound();
        laserColour = JB.Lib.CYAN;
        markerColour = JB.Lib.CYAN;
        ctGun = "M4";
    }

    public void SetRebel(CCSPlayerController? player)
    {
        if (!player.IsLegalAliveT())
            return;

        if (playerRebel.Contains(player) || JB.JailPlugin.EventActive() || JB.JailPlugin.lr.InLR(player))
            return;

        playerRebel.Add(player);

        if (Config.Prisoner.RebelAnnounce)
            Chat.LocalizeAnnounce(REBEL_PREFIX, $"lr.player_rebel", player.PlayerName);

        if (Config.Prisoner.RebelColor)
            player.SetColour(JB.Lib.RED);
    }

    public void GivePardon(CCSPlayerController? player)
    {
        if (player.IsLegalAlive() && player.IsT())
        {
            Chat.LocalizeAnnounce(Warden.WARDEN_PREFIX, "warden.give_pardon", player.PlayerName);
            player.SetColour(Color.FromArgb(255, 255, 255, 255));

            if (playerRebel.Contains(player))
            {
                RebelList.Remove(player);
                playerRebel.Remove(player);
            }
        }      
    }

    public void GiveFreeday(CCSPlayerController? player)
    {
        if (player.IsLegalAlive() && player.IsT())
        {
            // Check if the player has already received a freeday
            if (playerFreeday.Contains(player))
            {
                Chat.LocalizeAnnounce(Warden.WARDEN_PREFIX, "warden.already_has_freeday", player.PlayerName);
                return;
            }

            Chat.LocalizeAnnounce(Warden.WARDEN_PREFIX, "warden.give_freeday", player.PlayerName);

            player.SetColour(JB.Lib.GREEN);

            // Remove player from rebel lists if they were a rebel
            if (playerRebel.Contains(player))
            {
                RebelList.Remove(player);
                playerRebel.Remove(player);
            }

            // Add player to the list of players who have received a freeday
            playerFreeday.Add(player);
        }
    }

    public void FreedayDeath(CCSPlayerController? player, CCSPlayerController? killer)
    {
        if (JB.JailPlugin.EventActive())
            return;

        if (!player.IsLegal() || !killer.IsLegal())
            return;

        player.SetColour(Color.FromArgb(255, 255, 255, 255));
    }
    public void RebelDeath(CCSPlayerController? player, CCSPlayerController? killer)
    {
        if (JB.JailPlugin.EventActive())
            return;

        if (!player.IsLegal() || !killer.IsLegal())
            return;

        if (RebelList.TryGetValue(player, out var associatedKiller))
        {
            if (associatedKiller != killer)
                return;

            player.SetColour(Color.FromArgb(255, 255, 255, 255));

            if (playerRebel.Contains(player) && killer.IsCt())
            {
                if (Config.Prisoner.RebelAnnounce)
                    Chat.LocalizeAnnounce(REBEL_PREFIX, "rebel.kill", killer.PlayerName, player.PlayerName);
            }
            
            RebelList.Remove(player);
            playerRebel.Remove(player);

            // Chat.LocalizeAnnounce(REBEL_PREFIX, $"lr.player_notrebel", player.PlayerName);

            return;
        } 
    }


    public void RebelWeaponFire(CCSPlayerController? player, String weapon)
    {
        /*if (Config.T.RebelRequireHit)
            return;

        if (isRebel(player))
            return;

        // ignore weapons players are meant to have
        if (!weapon.Contains("knife") && !weapon.Contains("c4"))
            SetRebel(player);*/
    }

    public void PlayerHurt(CCSPlayerController? player, CCSPlayerController? attacker, int health, int damage)
    {
        if (!player.IsLegal())
            return;
        
        bool isWorld = !attacker.IsLegal();

        string localKey = health > 0 ? "logs.format.damage" : "logs.format.kill";

        if (isWorld) JB.JailPlugin.logs.AddLocalized(player, localKey + "_self", damage);
        else JB.JailPlugin.logs.AddLocalized(player, attacker!, localKey, damage);

        if (isWorld)
            return;

        if (player.IsCt() && attacker.IsT())
        {
            if (RebelList.TryGetValue(attacker, out CCSPlayerController? victim))
            {
                if (victim == player)
                    return;
            }

            if (!RebelList.ContainsKey(attacker))
            {
                RebelList.Add(attacker, player);
                SetRebel(attacker);
            }
        }
        else if (attacker.IsCt())
            Chat.PrintConsoleAll($"CT {attacker.PlayerName} hit {player.PlayerName} for {damage}");
    }

    public bool isRebel(CCSPlayerController? player)
    {
        return player.IsT() && playerRebel.Contains(player);
    }

    public static String REBEL_PREFIX = $" {ChatColors.Green}[REBEL]: {ChatColors.White}";

    public static JailConfig Config = new JailConfig();
    public static HashSet<CCSPlayerController> playerRebel = new HashSet<CCSPlayerController>();
    public static HashSet<CCSPlayerController> playerFreeday = new HashSet<CCSPlayerController>();
    public static Dictionary<CCSPlayerController, CCSPlayerController> RebelList = new Dictionary<CCSPlayerController, CCSPlayerController>();

    public Color laserColour { get; private set; } = JB.Lib.CYAN;
    public Color markerColour { get; private set; } = JB.Lib.CYAN;
    bool cached = false;

    public String ctGun = "M4";
}
