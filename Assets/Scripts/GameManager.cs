using UnityEngine;
using UnityEngine.InputSystem; // Using the new Input System
using UnityEngine.SceneManagement; // For reloading scenes or going back to menu
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

// Manages the main game loop, player turns, product placement, customer spawning, and scoring.
public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        Initializing,
        WaitingForPlacement, // Player needs to place their product
        AllProductsPlaced, // All players have placed, ready to spawn customers
        SpawningCustomers,
        CalculatingScores,
        RoundOver,
        GameOver
    }

    // Inspector Variables
    [Header("References")]
    [Tooltip("Reference to the UIManager script")]
    [SerializeField] private UIManager uiManager;
    [Tooltip("The RectTransform of the area where products/customers can be placed (e.g., the map panel)")]
    [SerializeField] private RectTransform placementArea;
    [Tooltip("The main camera used for screen-to-world point conversion (Needed for World Space/Screen Space Camera Canvas)")]
    [SerializeField] private Camera mainCamera;

    [Header("Prefabs")]
    [Tooltip("Prefab for the player's product marker")]
    [SerializeField] private GameObject playerProductPrefab;
    [Tooltip("Prefab for the customer marker")]
    [SerializeField] private GameObject customerPrefab;

    [Header("Game Settings")]
    [Tooltip("How many customers to spawn each round")]
    [SerializeField] private int customersPerRound = 10;
    [Tooltip("Delay before starting the next round after showing scores")]
    [SerializeField] private float endRoundDelay = 1.0f;
    [Tooltip("Assign unique colors for each potential player")]
    [SerializeField] private Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };

    // Private Variables
    private GameState currentState;
    private List<PlayerData> players = new List<PlayerData>();
    private List<GameObject> customerInstances = new List<GameObject>();
    private int currentPlayerIndex = 0;
    private bool isInputEnabled = false; // Control when player can click to place
    private Canvas parentCanvas; // To determine render mode

        void Start()
    {
        // Null Checks
        bool error = false;
        if (uiManager == null) { Debug.LogError("UIManager reference is missing!"); error = true; }
        if (placementArea == null) { Debug.LogError("Placement Area RectTransform is missing!"); error = true; }
        if (playerProductPrefab == null) { Debug.LogError("Player Product Prefab is missing!"); error = true; }
        if (customerPrefab == null) { Debug.LogError("Customer Prefab is missing!"); error = true; }

        // Attempt to find main camera if not assigned
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogWarning("Main Camera could not be found automatically. Assign it in the inspector if needed for certain Canvas modes.");

        // Find the parent Canvas
        if (placementArea != null) parentCanvas = placementArea.GetComponentInParent<Canvas>();
        if (parentCanvas == null) { Debug.LogError("Placement Area must be within a Canvas!"); error = true; }

        if (error)
        {
            Debug.LogError("GAME MANAGER SETUP ERRORS FOUND - Functionality may be impaired.");
            this.enabled = false; // Disable script if setup is wrong
            return;
        }

        customersPerRound = GameSettings.CustomersPerRound; // Get from static settings

        InitializeGame();
        SetupButtonListeners();
    }

    void Update()
    {
        // Handle input only when waiting for placement and input is enabled
        if (currentState == GameState.WaitingForPlacement && isInputEnabled)
        {
            HandlePlacementInput();
        }
    }

    /// Sets up the initial game state, players, and UI.
    private void InitializeGame()
    {
        Debug.Log("Initializing Game...");
        currentState = GameState.Initializing;
        uiManager.SetGameStateText("Initializing...");

        // Clear any previous game objects
        ClearMarkers(players.Select(p => p.ProductMarkerInstance).Where(m => m != null).ToList()); // Clear old player markers safely
        ClearMarkers(customerInstances); // Clear old customer markers

        // Create player data
        players.Clear();
        int numberOfPlayers = GameSettings.NumberOfPlayers > 0 ? GameSettings.NumberOfPlayers : 2; // Ensure at least 1 player, default 2
        if (GameSettings.NumberOfPlayers <= 0) Debug.LogWarning("Number of players was zero or negative, defaulting to 2.");

        for (int i = 0; i < numberOfPlayers; i++)
        {
            // Cycle through colors if not enough are defined
            Color playerColor = (playerColors != null && playerColors.Length > 0) ? playerColors[i % playerColors.Length] : Color.white;
            players.Add(new PlayerData(i, playerColor));
        }
        Debug.Log($"Created {players.Count} players.");

        // Reset scores (important for "New Game")
        foreach (var player in players)
        {
            player.Score = 0;
        }


        currentPlayerIndex = 0;
        uiManager.UpdateScoreboard(players); // Show initial scoreboard (scores 0)
        uiManager.SetButtonStates(showCreateCustomers: false, showRoundEndOptions: false);

        StartPlacementPhase();
    }

    // Sets up listeners for the UI buttons managed by UIManager.
    private void SetupButtonListeners()
    {
        if (uiManager == null) return;

        // Remove previous listeners first to prevent duplicates if game restarts
        uiManager.GetCreateCustomersButton()?.onClick.RemoveAllListeners();
        uiManager.GetPlayAgainButton()?.onClick.RemoveAllListeners();
        uiManager.GetNewGameButton()?.onClick.RemoveAllListeners();
        uiManager.GetExitButton()?.onClick.RemoveAllListeners();

        // Add new listeners
        uiManager.GetCreateCustomersButton()?.onClick.AddListener(OnCreateCustomersClicked);
        uiManager.GetPlayAgainButton()?.onClick.AddListener(OnPlayAgainClicked);
        uiManager.GetNewGameButton()?.onClick.AddListener(OnNewGameClicked);
        uiManager.GetExitButton()?.onClick.AddListener(OnExitClicked);
    }


    // Starts the phase where players place their products.
    private void StartPlacementPhase()
    {
        Debug.Log("Starting Placement Phase.");
        currentState = GameState.WaitingForPlacement;
        isInputEnabled = true; // Allow clicking
        currentPlayerIndex = 0; // Start with the first player
        ClearMarkers(customerInstances); // Clear customers from previous round if any

        // Reset product positions and destroy old markers if playing again
        foreach (var player in players)
        {
            if (player.ProductMarkerInstance != null)
            {
                Destroy(player.ProductMarkerInstance);
            }
            // Reset marker reference and position for the new round
            player.ProductMarkerInstance = null;
            player.ProductPosition = Vector2.zero;
        }


        UpdateTurnIndicator();
        uiManager.SetGameStateText("Place your products!");
        uiManager.SetButtonStates(showCreateCustomers: false, showRoundEndOptions: false);
        uiManager.UpdateScoreboard(players); // Update scoreboard (scores might be from previous round if "Play Again")
    }

    // Updates the UI to show whose turn it is.
    private void UpdateTurnIndicator()
    {
        if (currentPlayerIndex < players.Count)
        {
            uiManager.SetTurnIndicator($"{players[currentPlayerIndex].PlayerName}'s Turn");
        }
        else
        {
            uiManager.SetTurnIndicator("All products placed!");
        }
    }

    // Moves to the next player's turn or transitions state if all players are done.
    private void NextPlayerTurn()
    {
        // Ensure the current player actually placed a marker before moving on
        if (currentPlayerIndex >= players.Count || players[currentPlayerIndex].ProductMarkerInstance == null)
        {
            Debug.LogWarning($"Attempted to advance turn, but current player ({currentPlayerIndex}) hasn't placed a marker.");
            return;
        }


        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count)
        {
            // All players have placed their products
            Debug.Log("All players have placed products.");
            currentState = GameState.AllProductsPlaced;
            isInputEnabled = false; // Disable placement clicking
            uiManager.SetTurnIndicator("All products placed!");
            uiManager.SetGameStateText("Ready to create customers.");
            uiManager.SetButtonStates(showCreateCustomers: true, showRoundEndOptions: false); // Show the 'Create Customers' button
        }
        else
        {
            // Next player's turn
            UpdateTurnIndicator();
        }
    }

    // Spawns customer markers randomly within the placement area.
    private void SpawnCustomers()
    {
        Debug.Log($"Spawning {customersPerRound} customers...");
        currentState = GameState.SpawningCustomers;
        uiManager.SetGameStateText("Spawning Customers...");
        uiManager.SetButtonStates(showCreateCustomers: false, showRoundEndOptions: false); // Hide button during spawning

        ClearMarkers(customerInstances); // Clear previous customers

        if (customerPrefab == null || placementArea == null)
        {
            Debug.LogError("Cannot spawn customers: Prefab or Placement Area missing.");
            EndRound(); // Go to end round state if setup is broken
            return;
        }

        Rect mapRect = placementArea.rect;


        for (int i = 0; i < customersPerRound; i++)
        {
            // Generate random position within the RectTransform's local bounds
            float randomX = Random.Range(mapRect.xMin, mapRect.xMax);
            float randomY = Random.Range(mapRect.yMin, mapRect.yMax);
            Vector2 localSpawnPos = new Vector2(randomX, randomY);

            // Instantiate the customer prefab as a child of the placement area
            GameObject customerGO = Instantiate(customerPrefab, placementArea.transform);

            // Set Customer Position
            // Set its local position using the generated coordinates
            // Add a small negative Z offset to ensure they are behind player markers if needed
            customerGO.transform.localPosition = new Vector3(localSpawnPos.x, localSpawnPos.y, 0.1f);

            customerInstances.Add(customerGO);
        }
        Debug.Log($"{customerInstances.Count} customers spawned.");

        CalculateScores();
    }

    // Calculates scores based on proximity of products to customers.
    private void CalculateScores()
    {
        Debug.Log("Calculating Scores...");
        currentState = GameState.CalculatingScores;
        uiManager.SetGameStateText("Calculating Scores...");

        // Accumulate scores across rounds. Scores are only reset on New Game (in InitializeGame).

        if (customerInstances.Count == 0)
        {
            Debug.LogWarning("No customers spawned, skipping score calculation.");
            EndRound(); // Proceed to end round state
            return;
        }
        if (players.Count == 0 || players.All(p => p.ProductMarkerInstance == null))
        {
            Debug.LogWarning("No players or no player products placed, skipping score calculation.");
            EndRound(); // Proceed to end round state
            return;
        }


        foreach (GameObject customerGO in customerInstances)
        {
            if (customerGO == null) continue;

            float minDistanceSqr = float.MaxValue; // Use squared distance for efficiency
            PlayerData closestPlayer = null;
            Vector2 customerPos = customerGO.transform.localPosition; // Use local position relative to placementArea

            foreach (PlayerData player in players)
            {
                // Ensure player has actually placed a product this round
                if (player.ProductMarkerInstance == null) continue;

                // Calculate squared distance
                float distanceSqr = (player.ProductPosition - customerPos).sqrMagnitude;

                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                closestPlayer.Score++;
            }
            else
            {
                Debug.LogWarning($"No closest player found for customer at {customerPos}.");
            }
        }

        Debug.Log("Score calculation complete.");
        uiManager.UpdateScoreboard(players); // Update UI with new scores
        EndRound(); // Move to the round over state
    }

    // Transitions to the Round Over state, showing final scores and options.
    private void EndRound()
    {
        Debug.Log("Round Over.");
        currentState = GameState.RoundOver;
        isInputEnabled = false;
        uiManager.SetGameStateText("Round Over!");
        uiManager.SetTurnIndicator(""); // Clear turn indicator
        // Show round end options after a short delay
        StartCoroutine(ShowEndRoundOptionsAfterDelay(endRoundDelay));

    }

    private IEnumerator ShowEndRoundOptionsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == GameState.RoundOver) // Check if state hasn't changed (e.g. user clicked New Game quickly)
        {
            uiManager.SetButtonStates(showCreateCustomers: false, showRoundEndOptions: true);
        }
    }


    // Input Handling
    private void HandlePlacementInput()
    {
        // Check for left mouse button click
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return; // No click this frame
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 localPoint; // Variable to store the calculated local point

        // Determine the camera to use based on the Canvas RenderMode
        Camera eventCamera = null; // Default for Screen Space - Overlay
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            eventCamera = parentCanvas.worldCamera ?? mainCamera; // Use Canvas camera, fallback to main camera
            if (eventCamera == null)
            {
                Debug.LogError($"Canvas Render Mode is {parentCanvas.renderMode} but no camera is assigned to the Canvas or found as MainCamera!");
                uiManager.ShowMessageBox("Internal Error: UI Camera not found.");
                return;
            }
        }
        // else RenderMode is ScreenSpaceOverlay, eventCamera remains null


        // Convert screen position to local position within the placement area's RectTransform
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                placementArea, // The RectTransform to check against
                screenPosition,  // The mouse position
                eventCamera,     // The camera associated with the Canvas (null for Overlay)
                out localPoint); // The output local position

        // Check if the conversion was successful and the point is inside the rect
        if (isInside && placementArea.rect.Contains(localPoint))
        {
            PlaceProduct(localPoint);
        }
        else
        {
            if (!isInside)
            {
                Debug.LogWarning("ScreenPointToLocalPointInRectangle failed. Check camera setup and RectTransform.");
                uiManager.ShowMessageBox("Could not determine click position accurately.");
            }
            else
            {
                // isInside was true, but Contains was false
                Debug.LogWarning($"Clicked OUTSIDE Placement Area bounds. Local Point: {localPoint}, Area Rect: {placementArea.rect}");
                uiManager.ShowMessageBox("Please click inside the map area.");
            }
        }
    }


    // Places the current player's product marker at the specified local position
    // Handles instantiation, positioning, scaling, coloring, and visibility checks
    private void PlaceProduct(Vector2 localPosition)
    {
        if (currentState != GameState.WaitingForPlacement || currentPlayerIndex >= players.Count)
        {
            Debug.LogWarning($"Cannot place product. State: {currentState}, Player Index: {currentPlayerIndex}");
            return;
        }

        PlayerData currentPlayer = players[currentPlayerIndex];
        GameObject markerInstance = currentPlayer.ProductMarkerInstance; // Get existing instance if any

        // Instantiate or Move Marker
        if (markerInstance == null) // Instantiate new marker
        {
            Debug.Log($"{currentPlayer.PlayerName} placed product at {localPosition}");
            markerInstance = Instantiate(playerProductPrefab, placementArea.transform);
            if (markerInstance == null)
            {
                Debug.LogError("Failed to Instantiate playerProductPrefab!");
                return; // Critical error, cannot proceed
            }
            currentPlayer.ProductMarkerInstance = markerInstance; // Store reference immediately after successful instantiation
        }
        else // Marker already exists, just update position
        {
            Debug.Log($"{currentPlayer.PlayerName} repositioned product to {localPosition}");
        }

        // Set local position (X, Y) and ensure Z is slightly negative
        markerInstance.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.1f);


        // Customize Marker Appearance
        Color finalColor = currentPlayer.PlayerColor;
        finalColor.a = 1f; // Ensure alpha is fully opaque

        // Try getting SpriteRenderer first
        SpriteRenderer sr = markerInstance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true; // Ensure renderer is enabled
            sr.color = finalColor;
        }
        else
        {
            // Fallback to UI Image if no SpriteRenderer found
            Image img = markerInstance.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.enabled = true; // Ensure image is enabled
                img.color = finalColor;
            }
            else
            {
                Debug.LogWarning($"Could not find enabled SpriteRenderer or Image on Player Product Prefab '{markerInstance.name}' to set color/visibility.");
            }
        }

        // Optional: Add player number label
        TextMeshPro tmp = markerInstance.GetComponentInChildren<TextMeshPro>(); // For 3D TextMeshPro
        TextMeshProUGUI tmpUGUI = markerInstance.GetComponentInChildren<TextMeshProUGUI>(); // For UI TextMeshPro
        if (tmp != null)
        {
            tmp.enabled = true;
            tmp.text = (currentPlayer.PlayerId + 1).ToString();
            tmp.color = Color.white; // Or a contrasting color
        }
        else if (tmpUGUI != null)
        {
            tmpUGUI.enabled = true;
            tmpUGUI.text = (currentPlayer.PlayerId + 1).ToString();
            tmpUGUI.color = Color.white; // Or a contrasting color
        }

        if (!markerInstance.activeInHierarchy)
        {
            Debug.LogWarning($"Marker instance '{markerInstance.name}' is not active in hierarchy after placement!");
        }
        Renderer renderer = markerInstance.GetComponentInChildren<Renderer>(); // General renderer check
        if (renderer != null && !renderer.enabled)
        {
            Debug.LogWarning($"Renderer on marker instance '{markerInstance.name}' is disabled!");
        }
        Graphic graphic = markerInstance.GetComponentInChildren<Graphic>(); // General UI graphic check
        if (graphic != null && !graphic.enabled)
        {
            Debug.LogWarning($"Graphic on marker instance '{markerInstance.name}' is disabled!");
        }


        // Store the confirmed position in player data
        currentPlayer.ProductPosition = localPosition;

        // Move to the next player's turn ONLY after successful placement/update
        NextPlayerTurn();
    }


    // Button Event Handlers

    private void OnCreateCustomersClicked()
    {
        if (currentState == GameState.AllProductsPlaced)
        {
            SpawnCustomers();
        }
        else
        {
            Debug.LogWarning($"Create Customers button clicked in unexpected state: {currentState}");
        }
    }

    private void OnPlayAgainClicked()
    {
        Debug.Log("Play Again Clicked - Restarting placement phase with current scores.");
        // Restart the placement phase, keeping current scores
        StartPlacementPhase();
    }

    private void OnNewGameClicked()
    {
        Debug.Log("New Game Clicked - Reloading Start Menu scene.");
        // Go back to the start menu scene
        SceneManager.LoadScene("StartScene");
    }

    private void OnExitClicked()
    {
        Debug.Log("Exit Clicked");
        Application.Quit();

#if UNITY_EDITOR
        // Stop playing the scene in the editor for convenience
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


    // Destroys a list of GameObjects safely.
    private void ClearMarkers(List<GameObject> markers)
    {
        if (markers == null) return;

        // Iterate backwards when removing items from a list being iterated
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            GameObject marker = markers[i];
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        markers.Clear(); // Clear the list references
    }
}
