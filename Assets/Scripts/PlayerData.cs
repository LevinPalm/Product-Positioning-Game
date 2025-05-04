using UnityEngine;

// Holds data for a single player.
[System.Serializable]
public class PlayerData
{
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; } // e.g., "Player 1"
    public Color PlayerColor { get; private set; } // To distinguish markers
    public Vector2 ProductPosition { get; set; }
    public int Score { get; set; }
    public GameObject ProductMarkerInstance { get; set; } // Reference to the instantiated marker

    // Constructor
    public PlayerData(int id, Color color)
    {
        PlayerId = id;
        PlayerName = "Player " + (id + 1); // Player IDs start from 0, display as 1-based
        PlayerColor = color;
        Score = 0;
        ProductPosition = Vector2.zero; // Default position
        ProductMarkerInstance = null;
    }
}
