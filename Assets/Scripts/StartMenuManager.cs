using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


// Manages the UI interactions and logic for the Start Scene.
public class StartMenuManager : MonoBehaviour
{
    // Inspector Variables
    [Header("UI Elements")]
    [Tooltip("Input field for the number of players")]
    [SerializeField] private TMP_InputField playerCountInput;

    [Tooltip("Input field for the X-axis description")]
    [SerializeField] private TMP_InputField xAxisInput;

    [Tooltip("Input field for the Y-axis description")]
    [SerializeField] private TMP_InputField yAxisInput;

    [Tooltip("Button to start the game")]
    [SerializeField] private Button playButton;

    [Header("Scene Management")]
    [Tooltip("Name of the main game scene to load")]
    [SerializeField] private string gameSceneName = "GameScene";


    void Start()
    {
        // Initialize UI fields with default or previously set values
        if (playerCountInput != null)
        {
            playerCountInput.text = GameSettings.NumberOfPlayers.ToString();
        }
        if (xAxisInput != null)
        {
            xAxisInput.text = GameSettings.XAxisLabel;
        }
        if (yAxisInput != null)
        {
            yAxisInput.text = GameSettings.YAxisLabel;
        }

        // Add listener to the play button
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogError("Play Button is not assigned in the Inspector!");
        }

        if (playerCountInput != null)
        {
            playerCountInput.onValueChanged.AddListener(ValidatePlayerCount);
        }
    }

    void OnDestroy()
    {
        // Clean up listeners when the object is destroyed
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(StartGame);
        }
         if (playerCountInput != null)
        {
            playerCountInput.onValueChanged.RemoveListener(ValidatePlayerCount);
        }
    }


    // Called when the Play button is clicked
    // Stores settings and loads the game scene
    public void StartGame()
    {
        // Store Settings
        // Player Count
        if (int.TryParse(playerCountInput.text, out int playerCount) && playerCount > 0)
        {
            GameSettings.NumberOfPlayers = playerCount;
        }
        else
        {
            Debug.LogWarning("Invalid player count input. Using default: " + GameSettings.NumberOfPlayers);
            playerCountInput.text = GameSettings.NumberOfPlayers.ToString(); // Reset input field
        }

        // Axis Labels
        GameSettings.XAxisLabel = xAxisInput.text;
        GameSettings.YAxisLabel = yAxisInput.text;

        // Load Game Scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log($"Starting game with {GameSettings.NumberOfPlayers} players. X: '{GameSettings.XAxisLabel}', Y: '{GameSettings.YAxisLabel}'. Loading scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene Name is not set in the Inspector!");
        }
    }


    // Validates the player count input
    private void ValidatePlayerCount(string input)
    {
        if (int.TryParse(input, out int count))
        {
            if (count <= 0)
            {
                Debug.LogWarning("Player count must be greater than 0.");
                // disable the play button if invalid
                playButton.interactable = false;
            }
            else
            {
                // Input is valid
                playButton.interactable = true;
            }
        }
        else if (!string.IsNullOrEmpty(input)) // If input is not empty and not a number
        {
            Debug.LogWarning("Player count must be a number.");
            playButton.interactable = false;
        }
        else // If input is empty
        {
             playButton.interactable = false;
        }
    }
}
