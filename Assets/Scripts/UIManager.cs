using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq; // Required for OrderByDescending
using TMPro;

// Manages the UI elements in the Game Scene.
public class UIManager : MonoBehaviour
{
    // Inspector Variables
    [Header("Axis Labels")]
    [SerializeField] private TextMeshProUGUI xAxisLabelText;
    [SerializeField] private TextMeshProUGUI yAxisLabelText;

    [Header("Game State & Turn Info")]
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private TextMeshProUGUI gameStateText;

    [Header("Scoreboard")]
    [SerializeField] private GameObject scoreboardPanel; // The parent panel for the scoreboard
    [SerializeField] private GameObject scoreEntryPrefab; // Prefab for a single player's score line (Text + Text)
    [SerializeField] private Transform scoreEntryContainer; // Layout group container for score entries

    [Header("Buttons")]
    [SerializeField] private Button createCustomersButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;

    [Header("Message Box (Optional)")]
    [SerializeField] private GameObject messageBoxPanel;
    [SerializeField] private TextMeshProUGUI messageBoxText;
    [SerializeField] private Button messageBoxOkButton;

    private List<GameObject> scoreEntries = new List<GameObject>(); // Keep track of instantiated score entries

    void Start()
    {
        // Initial setup
        UpdateAxisLabels(GameSettings.XAxisLabel, GameSettings.YAxisLabel);
        SetTurnIndicator(""); // Clear initially
        SetGameStateText(""); // Clear initially

        // Initially hide round-end buttons and scoreboard details
        SetButtonVisibility(createCustomersButton, false); // Hide until all players place products
        SetButtonVisibility(playAgainButton, false);
        SetButtonVisibility(newGameButton, false);
        SetButtonVisibility(exitButton, true); // Keep exit always visible

        if (scoreboardPanel != null) scoreboardPanel.SetActive(false); // Hide scoreboard initially
        if (messageBoxPanel != null) messageBoxPanel.SetActive(false); // Hide message box

        // Add listener for message box OK button if it exists
         if (messageBoxOkButton != null)
         {
             messageBoxOkButton.onClick.AddListener(HideMessageBox);
         }
    }

    // Updates the text labels for the X and Y axes.
    public void UpdateAxisLabels(string xLabel, string yLabel)
    {
        if (xAxisLabelText != null) xAxisLabelText.text = xLabel;
        if (yAxisLabelText != null) yAxisLabelText.text = yLabel;
    }

    // Sets the text indicating whose turn it is or the current action.
    public void SetTurnIndicator(string text)
    {
        if (turnIndicatorText != null) turnIndicatorText.text = text;
    }

    // Sets the text indicating the overall game state.
    public void SetGameStateText(string text)
    {
        if (gameStateText != null)
        {
             gameStateText.text = text;
             gameStateText.gameObject.SetActive(!string.IsNullOrEmpty(text)); // Show only if text is present
        }
    }

    // Updates the scoreboard display based on player data.
    public void UpdateScoreboard(List<PlayerData> players)
    {
        if (scoreEntryPrefab == null || scoreEntryContainer == null || scoreboardPanel == null)
        {
            Debug.LogError("Scoreboard UI elements not assigned in Inspector!");
            return;
        }

        scoreboardPanel.SetActive(true); // Show the scoreboard

        // Clear existing entries
        foreach (GameObject entry in scoreEntries)
        {
            Destroy(entry);
        }
        scoreEntries.Clear();

        // Sort players by score (descending)
        List<PlayerData> sortedPlayers = players.OrderByDescending(p => p.Score).ToList();

        // Create new entries
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            PlayerData player = sortedPlayers[i];
            GameObject entryInstance = Instantiate(scoreEntryPrefab, scoreEntryContainer);
            entryInstance.SetActive(true);

            TextMeshProUGUI[] texts = entryInstance.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = $"{i + 1}. {player.PlayerName}" + ":"; // Rank + Name
                texts[1].text = player.Score.ToString();
                texts[0].color = player.PlayerColor; // Color the name text
                texts[1].color = player.PlayerColor; // Color the score text
            }
            else
            {
                Debug.LogWarning("Score Entry Prefab doesn't have enough TextMeshProUGUI components.");
                // Fallback: Use the first text component found
                 TextMeshProUGUI text = entryInstance.GetComponentInChildren<TextMeshProUGUI>();
                 if(text != null) text.text = $"{i + 1}. {player.PlayerName}: {player.Score}";
            }
            scoreEntries.Add(entryInstance);
        }
    }

    // Configures the visibility and interactivity of the main action buttons
    // <param name="showCreateCustomers">Show the 'Create Customers' button?</param>
    // <param name="showRoundEndOptions">Show 'Play Again', 'New Game', 'Exit' buttons?</param>
    public void SetButtonStates(bool showCreateCustomers, bool showRoundEndOptions)
    {
        SetButtonVisibility(createCustomersButton, showCreateCustomers);
        SetButtonVisibility(playAgainButton, showRoundEndOptions);
        SetButtonVisibility(newGameButton, showRoundEndOptions);
        //SetButtonVisibility(exitButton, showRoundEndOptions); // Show exit with other round end options
    }

    // Shows a simple message box with the given text.
    public void ShowMessageBox(string message)
    {
        if (messageBoxPanel != null && messageBoxText != null)
        {
            messageBoxText.text = message;
            messageBoxPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Message Box UI not set up. Message: " + message);
        }
    }

    // Hides the message box. Called by the OK button.
    public void HideMessageBox()
    {
         if (messageBoxPanel != null)
         {
             messageBoxPanel.SetActive(false);
         }
    }


    private void SetButtonVisibility(Button button, bool isVisible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(isVisible);
        }
    }

    // --- Button Click Handlers (to be connected in GameManager) ---
    // These methods are placeholders; the actual logic will be in GameManager.
    // GameManager will call these methods on the UIManager instance.
    // Alternatively, GameManager can add listeners directly to the buttons.
  
    public Button GetCreateCustomersButton() => createCustomersButton;
    public Button GetPlayAgainButton() => playAgainButton;
    public Button GetNewGameButton() => newGameButton;
    public Button GetExitButton() => exitButton;
    
}
