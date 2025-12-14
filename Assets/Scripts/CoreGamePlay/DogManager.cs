/// <summary>
/// Author: Jayden Wong
/// Date: 12 December 2025
/// Core game logic manager that orchestrates the 25-turn gameplay loop.
/// Handles button states, turn progression, stat modifications, fail chance
/// calculation, result panel display, and Firebase session logging.
/// Uses a "busy" state system to lock buttons during animations, and a
/// "pending action" pattern to defer stat changes until animations complete.
/// Calculates play failure probability based on energy/hunger zones (0%, 20%,
/// 40%, 70%, 80%, 90%, 100%) and provides turn-by-turn Firebase analytics.
/// Coordinates with DogController for animations, AudioManager for sounds,
/// and MenuManager for scene transitions.
/// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DogManager : MonoBehaviour
{
    public static DogManager Instance { get; private set; }

    [Header("UI Buttons")]
    // Three main gameplay action buttons
    [SerializeField] private Button playButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button feedButton;

    [Header("References")]
    // Dog's stat container (energy, hunger, affection)
    [SerializeField] private DogStats dogStats;

    [Header("Turn Settings")]
    // Maximum number of turns before game ends (typically 25)
    [SerializeField] private int maxTurns = 25;

    [Header("Result Panels")]
    // UI feedback panels shown after each action
    [SerializeField] private GameObject playSuccessPanel;
    [SerializeField] private TMP_Text  playSuccessText;

    [SerializeField] private GameObject playFailPanel;
    [SerializeField] private TMP_Text  playFailText;

    [SerializeField] private GameObject restResultPanel;
    [SerializeField] private TMP_Text  restResultText;

    [SerializeField] private GameObject feedResultPanel;
    [SerializeField] private TMP_Text  feedResultText;

    [Header("Game Over UI")]
    // End-game panel with final score and encouragement message
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Menu Reference")]
    // Reference to MenuManager for returning to main menu
    [SerializeField] private MenuManager menuManager;


    // Current turn number (1-25), publicly readable for UI display
    public int CurrentTurn { get; private set; } = 0;
    
    // Current probability (0-100%) that Play action will fail
    public float CurrentFailChance { get; private set; } = 0f;

    // Reference to the active dog controller
    private DogController currentDog;
    
    // True when animation is playing (locks buttons)
    private bool isBusy;

    // Stores which action is waiting to be resolved after animation
    private ActionType pendingAction = ActionType.None;
    
    // Reference to active result panel coroutine (for cancellation)
    private Coroutine panelRoutine;

    // Tracks affection change from last action for Firebase logging
    private int lastAffectionGain = 0;

    /// <summary>
    /// Internal enum for tracking which action is pending resolution.
    /// Used to defer stat changes until animations complete.
    /// </summary>
    private enum ActionType
    {
        None,
        PlaySuccess,
        PlayFail,
        Rest,
        Feed
    }

    /// <summary>
    /// Energy/hunger zones for fail chance calculation.
    /// Low: 0-30, Medium: 31-69, High: 70-100
    /// </summary>
    private enum Zone
    {
        Low,
        Medium,
        High
    }

    private void Awake()
    {
        // Singleton pattern - ensure only one DogManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Hide all result panels at game start for clean UI
        HideAllResultPanels();

        // Calculate initial fail chance based on starting stats
        RecalculateFailChance();
        
        // Set initial button states
        UpdateButtonInteractable();

        // Initialize Firebase session tracking
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.StartNewSession();
            Debug.Log("[DogManager] New Firebase session started");
        }
    }

    /// <summary>
    /// Called by DogController.OnEnable when dog is ready.
    /// Registers the dog and enables buttons for gameplay.
    /// </summary>
    public void RegisterDog(DogController dog)
    {
        currentDog = dog;
        UpdateButtonInteractable();
        Debug.Log("[DogManager] Dog registered, buttons enabled");
    }

    /// <summary>
    /// Controls button lock state during animations.
    /// Called by DogController: SetBusy(true) when animation starts,
    /// SetBusy(false) when animation ends (via Animation Event).
    /// When busy becomes false, triggers pending action resolution.
    /// </summary>
    public void SetBusy(bool busy)
    {
        isBusy = busy;
        UpdateButtonInteractable();

        // When animation finishes, resolve the pending action
        if (!busy && pendingAction != ActionType.None)
        {
            ResolvePendingAction();
        }
    }

    /// <summary>
    /// Updates button interactable states based on game state.
    /// Buttons are enabled only when: not busy, turns remaining, and dog exists.
    /// </summary>
    private void UpdateButtonInteractable()
    {
        bool canUse = !isBusy && CurrentTurn < maxTurns && currentDog != null;

        if (playButton != null) playButton.interactable = canUse;
        if (restButton != null) restButton.interactable = canUse;
        if (feedButton != null) feedButton.interactable = canUse;
    }

    /// <summary>
    /// Called by Back button on Game Over panel.
    /// Resets game state and returns to main menu via MenuManager.
    /// </summary>
    public void OnBackToMenuButton()
    {
        // Hide game over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Reset all game variables for potential replay
        ResetGameState();
        
        // Return to menu (MenuManager handles XR disable)
        if (menuManager != null)
        {
            menuManager.ShowMenuPanel();
        }
        else
        {
            Debug.LogError("[DogManager] MenuManager reference not assigned!");
        }
    }

    /// <summary>
    /// Resets all game variables to starting values.
    /// Called when returning to menu to prepare for next game session.
    /// </summary>
    private void ResetGameState()
    {
        CurrentTurn = 0;
        currentDog = null;
        
        // Reset dog stats to starting values (80 energy, 80 hunger, 10 affection)
        if (dogStats != null)
        {
            dogStats.energy = 80f;
            dogStats.hunger = 80f;
            dogStats.affection = 10;
        }
        
        RecalculateFailChance();
        UpdateButtonInteractable();
        HideAllResultPanels();
    }

    // UI BUTTON HANDLERS  -----------------------------------------------------

    /// <summary>
    /// Handles Play button click.
    /// Rolls random chance against CurrentFailChance to determine success/fail.
    /// Initiates animation and defers stat changes until animation completes.
    /// </summary>
    public void OnPlayButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns || dogStats == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        // Roll for success/failure based on current fail chance
        float failChance = CurrentFailChance;
        float roll = Random.Range(0f, 100f);
        bool failed = roll < failChance;

        // Store which outcome to apply after animation
        pendingAction = failed ? ActionType.PlayFail : ActionType.PlaySuccess;

        // Start animation (will lock buttons)
        currentDog.Play(failed);
    }

    /// <summary>
    /// Handles Rest button click.
    /// Initiates rest animation and defers stat changes until animation completes.
    /// </summary>
    public void OnRestButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        pendingAction = ActionType.Rest;
        currentDog.Rest();
    }

    /// <summary>
    /// Handles Feed button click.
    /// Initiates feed animation and defers stat changes until animation completes.
    /// </summary>
    public void OnFeedButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        pendingAction = ActionType.Feed;
        currentDog.Feed();
    }

    // GAME LOGIC --------------------------------------------------------------

    /// <summary>
    /// Resolves the pending action after animation completes.
    /// Applies stat changes, increments turn counter, logs to Firebase,
    /// checks for game over condition, and recalculates fail chance.
    /// </summary>
    private void ResolvePendingAction()
    {
        if (dogStats == null) return;

        // Apply stat changes based on action type
        switch (pendingAction)
        {
            case ActionType.PlaySuccess:
                ApplyPlaySuccess();
                break;
            case ActionType.PlayFail:
                ApplyPlayFail();
                break;
            case ActionType.Rest:
                ApplyRest();
                break;
            case ActionType.Feed:
                ApplyFeed();
                break;
        }
        
        // Increment turn counter
        CurrentTurn++;
        
        // Log this turn to Firebase for analytics
        LogCurrentTurnToFirebase();

        // Clear pending action
        pendingAction = ActionType.None;

        // Check if game is over (reached max turns)
        if (CurrentTurn >= maxTurns)
        {
            // Disable buttons to prevent further actions
            UpdateButtonInteractable();

            // Play game over sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGameOver();

            // Switch UI mode (MenuManager handles this)
            if (menuManager != null)
                menuManager.SetUIMode(true);

            // End Firebase session and save final data
            if (FirebaseDatabaseManager.Instance != null)
            {
                FirebaseDatabaseManager.Instance.EndSession(dogStats.affection);
                Debug.Log("[DogManager] Game ended. Session saved to Firebase");
            }

            // Show game over panel
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            // Display final score with encouraging message
            if (finalScoreText != null)
            {
                string encouragement = GetEncouragementText(dogStats.affection);
                finalScoreText.text = $"Your Final Affection: {dogStats.affection}\n\n{encouragement}";
            }

            return;
        }

        // Recalculate fail chance for next Play action
        RecalculateFailChance();
    }

    /// <summary>
    /// Returns encouraging message based on final affection score.
    /// Provides tiered feedback to motivate players.
    /// </summary>
    private string GetEncouragementText(int affection)
    {
        if (affection >= 100)
            return "Perfect bond!\nYour dog adores you!";
        else if (affection >= 80)
            return "Amazing!\nYour dog is very happy!";
        else if (affection >= 60)
            return "Great job!\nYour dog loves spending time with you!";
        else if (affection >= 40)
            return "Good effort!\nKeep building that bond!";
        else if (affection >= 20)
            return "Not bad!\nTry balancing energy and hunger better!";
        else
            return "Keep trying!\nEvery bond takes time to build!";
    }

    /// <summary>
    /// Applies stat changes for successful Play action.
    /// Energy -20, Hunger -15, Affection +5 to +12 (random).
    /// </summary>
    private void ApplyPlaySuccess()
    {
        dogStats.energy -= 20f;
        dogStats.hunger -= 15f;

        // Random affection gain between 5 and 12 (inclusive)
        int affectionGain = Random.Range(5, 13);
        dogStats.affection += affectionGain;

        // Store for Firebase logging
        lastAffectionGain = affectionGain;

        ClampStats();

        // Update result panel text
        if (playSuccessText != null)
        {
            playSuccessText.text = $"Your dog can't stop wagging their tail! Affection +{affectionGain}";
        }

        ShowResultPanelForSeconds(playSuccessPanel);
    }

    /// <summary>
    /// Applies stat changes for failed Play action.
    /// Energy -10, Hunger -10, Affection -3 (penalty).
    /// </summary>
    private void ApplyPlayFail()
    {
        dogStats.energy -= 10f;
        dogStats.hunger -= 10f;
        dogStats.affection -= 3;

        // Store negative affection change for Firebase logging
        lastAffectionGain = -3;

        ClampStats();

        // Update result panel text
        if (playFailText != null)
        {
            playFailText.text = "Your dog seems too tired or hungry to play right now. Affection -3";
        }

        ShowResultPanelForSeconds(playFailPanel);
    }

    /// <summary>
    /// Applies stat changes for Rest action.
    /// Energy +25, Hunger -5, no affection change.
    /// </summary>
    private void ApplyRest()
    {
        dogStats.energy += 25f;
        dogStats.hunger -= 5f;

        // No affection change for rest
        lastAffectionGain = 0;

        ClampStats();

        // Update result panel text
        if (restResultText != null)
        {
            restResultText.text = "Your dog curls up for a peaceful rest. Energy +25, Hunger −5.";
        }

        ShowResultPanelForSeconds(restResultPanel);
    }

    /// <summary>
    /// Applies stat changes for Feed action.
    /// Hunger +35, Energy +5, Affection +2.
    /// </summary>
    private void ApplyFeed()
    {
        dogStats.hunger += 35f;
        dogStats.energy += 5f;
        dogStats.affection += 2;

        // Store affection gain for Firebase logging
        lastAffectionGain = 2;

        ClampStats();

        // Update result panel text
        if (feedResultText != null)
        {
            feedResultText.text = "Your dog enjoys a tasty meal. Hunger +35, Energy +5, \nAffection +5.";
        }

        ShowResultPanelForSeconds(feedResultPanel);
    }

    /// <summary>
    /// Clamps energy and hunger to 0-100 range.
    /// Prevents affection from going below 0 (no upper limit).
    /// </summary>
    private void ClampStats()
    {
        dogStats.energy = Mathf.Clamp(dogStats.energy, 0f, 100f);
        dogStats.hunger = Mathf.Clamp(dogStats.hunger, 0f, 100f);

        if (dogStats.affection < 0)
            dogStats.affection = 0;
    }

    /// <summary>
    /// Recalculates play fail chance based on current energy and hunger.
    /// Uses zone-based lookup table (Low/Medium/High thresholds).
    /// </summary>
    private void RecalculateFailChance()
    {
        if (dogStats == null)
        {
            CurrentFailChance = 0f;
            return;
        }

        float energy = dogStats.energy;
        float hunger = dogStats.hunger;

        CurrentFailChance = ComputeFailChance(energy, hunger);
    }

    /// <summary>
    /// Determines which zone (Low/Medium/High) a stat value falls into.
    /// High: 70-100, Medium: 31-69, Low: 0-30
    /// </summary>
    private Zone GetZone(float value)
    {
        if (value >= 70f) return Zone.High;
        if (value >= 31f) return Zone.Medium;
        return Zone.Low;
    }

    /// <summary>
    /// Calculates fail chance percentage based on energy/hunger zones.
    /// Uses predefined lookup table for all zone combinations.
    /// Special cases: 100% if exhausted/starving, 0% if perfect condition.
    /// </summary>
    private float ComputeFailChance(float energy, float hunger)
    {
        // Special case: dog is exhausted or starving - Play always fails
        if (energy <= 0f || hunger <= 0f)
            return 100f;

        // Special case: perfect condition (both at 100)
        if (Mathf.Approximately(energy, 100f) && Mathf.Approximately(hunger, 100f))
            return 0f;

        // Determine zones for both stats
        Zone e = GetZone(energy);
        Zone h = GetZone(hunger);

        // Lookup table based on zone combinations
        if (e == Zone.High && h == Zone.High) return 0f;
        if (e == Zone.High && h == Zone.Medium) return 20f;
        if (e == Zone.Medium && h == Zone.High) return 20f;
        if (e == Zone.Medium && h == Zone.Medium) return 40f;
        if (e == Zone.Low && h == Zone.High) return 70f;
        if (e == Zone.High && h == Zone.Low) return 70f;
        if (e == Zone.Medium && h == Zone.Low) return 80f;
        if (e == Zone.Low && h == Zone.Medium) return 80f;
        if (e == Zone.Low && h == Zone.Low) return 90f;

        // Fallback (should never reach)
        return 0f;
    }

    // RESULT PANEL HELPERS ----------------------------------------------------

    /// <summary>
    /// Hides all result panels for clean UI state.
    /// </summary>
    private void HideAllResultPanels()
    {
        if (playSuccessPanel != null) playSuccessPanel.SetActive(false);
        if (playFailPanel != null)    playFailPanel.SetActive(false);
        if (restResultPanel != null)  restResultPanel.SetActive(false);
        if (feedResultPanel != null)  feedResultPanel.SetActive(false);
    }

    /// <summary>
    /// Shows a result panel for 3 seconds then automatically hides it.
    /// Cancels any previously running panel timer.
    /// </summary>
    private void ShowResultPanelForSeconds(GameObject panel)
    {
        if (panel == null) return;

        // Stop previous timer if one is running
        if (panelRoutine != null)
            StopCoroutine(panelRoutine);

        panelRoutine = StartCoroutine(ResultPanelRoutine(panel));
    }

    /// <summary>
    /// Coroutine that shows a panel for 3 seconds then hides it.
    /// </summary>
    private IEnumerator ResultPanelRoutine(GameObject panel)
    {
        // Hide all panels first for clean transition
        HideAllResultPanels();
        
        // Show the target panel
        panel.SetActive(true);

        // Wait 3 seconds
        yield return new WaitForSeconds(3f);

        // Hide the panel
        panel.SetActive(false);
        panelRoutine = null;
    }

    // FIREBASE INTEGRATION ----------------------------------------------------
    
    /// <summary>
    /// Logs the completed turn to Firebase with all relevant data.
    /// Captures action type, result, affection change, and final stat values.
    /// Called after each turn completes for turn-by-turn analytics.
    /// </summary>
    private void LogCurrentTurnToFirebase()
    {
        if (FirebaseDatabaseManager.Instance == null || dogStats == null)
            return;

        // Determine action and result strings based on completed action
        string action = "";
        string result = "";

        switch (pendingAction)
        {
            case ActionType.PlaySuccess:
                action = "Play";
                result = "Success";
                break;
                
            case ActionType.PlayFail:
                action = "Play";
                result = "Failed";
                break;
                
            case ActionType.Rest:
                action = "Rest";
                result = "Success";
                break;
                
            case ActionType.Feed:
                action = "Feed";
                result = "Success";
                break;
        }

        // Send turn data to Firebase
        FirebaseDatabaseManager.Instance.LogTurn(
            CurrentTurn,
            action,
            result,
            lastAffectionGain,
            dogStats.energy,
            dogStats.hunger
        );
    }
}