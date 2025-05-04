using UnityEngine;

// Static class to hold game settings that persist between scenes.
public static class GameSettings
{
    // Default values
    public static int NumberOfPlayers { get; set; } = 2; // Default to 2 players
    public static string XAxisLabel { get; set; } = "X-Axis Feature";
    public static string YAxisLabel { get; set; } = "Y-Axis Feature";
    public static int CustomersPerRound { get; set; } = 10; // Default number of customers
}
