using CounterStrikeSharp.API.Core;

public class LRRace : LRBase
{
    public LRRace(LastRequest manager,LastRequest.LRType type,int LRSlot, int playerSlot, String choice) : base(manager,type,LRSlot,playerSlot,choice)
    {

    }

    public override void InitPlayer(CCSPlayerController player)
    {    
        weaponRestrict = "";
   
    {
        

        if (player.IsLegalAlive())
        {
            player.SetHealth(1);



            switch (choice)
            {
                case "Vanilla":
                    break;

                case "Low gravity":
                    player.SetGravity(0.6f);
                    break;
            }
        }
    }    
using System;
using System.Collections.Generic;
using System.Linq;

namespace CS_RacePlugin
{
    public class RaceFeature
    {
        // Define the start and finish coordinates
        private static readonly float StartX = 1000.0f;
        private static readonly float StartY = 1000.0f;
        private static readonly float StartZ = 100.0f;

        private static readonly float FinishX = 2000.0f;
        private static readonly float FinishY = 2000.0f;
        private static readonly float FinishZ = 100.0f;

        // Flag to check if the race has started
        private bool raceStarted = false;

        // List to hold the players participating in the race
        private List<Player> raceParticipants = new List<Player>();

        // Method to start the race when a command is triggered
        public void StartRace(List<Player> players)
        {
            if (players.Count != 2)
            {
                Console.WriteLine("You need exactly 2 players to start the race!");
                return;
            }

            // Set up participants
            raceParticipants = players;

            // Teleport both players to the start point
            foreach (var player in raceParticipants)
            {
                TeleportToStartPoint(player);
            }

            raceStarted = true;
            Console.WriteLine("The race is about to begin!");

            // Start a race timer to check for the race completion
            var raceTimer = new System.Timers.Timer(1000);
            raceTimer.Elapsed += OnRaceTick;
            raceTimer.Start();
        }

        // Method to teleport players to the starting point
        private void TeleportToStartPoint(Player player)
        {
            player.Position = new Vector3(StartX, StartY, StartZ);
            Console.WriteLine($"Player {player.Name} teleported to the start point.");
        }

        // Method to calculate the player's distance from the finish line
        private float GetPlayerDistanceFromFinish(Player player)
        {
            return Vector3.Distance(player.Position, new Vector3(FinishX, FinishY, FinishZ));
        }

        // Method to check race progress and determine the last player
        private void OnRaceTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (raceStarted)
            {
                // Find out who is furthest from the finish line
                var lastPlayer = raceParticipants.OrderBy(p => GetPlayerDistanceFromFinish(p)).Last();

                // If the last player has reached the finish line, end the race
                if (GetPlayerDistanceFromFinish(lastPlayer) < 5.0f)
                {
                    raceStarted = false;
                    EndRace(lastPlayer);
                }
            }
        }

        // Method to end the race and kill the last player
        private void EndRace(Player lastPlayer)
        {
            // Kill the last player
            Console.WriteLine($"{lastPlayer.Name} finished last and has been eliminated.");
            KillPlayer(lastPlayer);
        }

        // Method to simulate killing the player (for race completion)
        private void KillPlayer(Player player)
        {
            // Set the player status as dead or simulate death
            player.IsDead = true;
            Console.WriteLine($"Player {player.Name} has been eliminated.");
        }
    }

    // Player class to simulate a player in the race
    public class Player
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public bool IsDead { get; set; }

        public Player(string name)
        {
            Name = name;
            Position = new Vector3(0, 0, 0);
            IsDead = false;
        }
    }

    // Vector3 class to represent 3D positions (for simplicity, 2D race can be used)
    public struct Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // Method to calculate the distance between two points
        public static float Distance(Vector3 v1, Vector3 v2)
        {
            return (float)Math.Sqrt(Math.Pow(v2.X - v1.X, 2) + Math.Pow(v2.Y - v1.Y, 2) + Math.Pow(v2.Z - v1.Z, 2));
        }
    }

    // Example usage
    public class Program
    {
        public static void Main()
        {
            var raceFeature = new RaceFeature();

            // Create some example players
            var player1 = new Player("Player1");
            var player2 = new Player("Player2");

            // Start the race with these two players
            raceFeature.StartRace(new List<Player> { player1, player2 });

            // Simulate players progressing through the race (this is just for testing)
            player1.Position = new Vector3(1500.0f, 1500.0f, 100.0f);
            player2.Position = new Vector3(1900.0f, 1900.0f, 100.0f);

            // Let the race run for a few seconds
            System.Threading.Thread.Sleep(5000);
        }
    }
}
