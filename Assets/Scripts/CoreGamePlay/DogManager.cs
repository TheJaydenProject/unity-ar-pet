using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DogManager : MonoBehaviour
{
    public static DogManager Instance { get; private set; }

    [Header("UI Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button feedButton;

    [Header("References")]
    [SerializeField] private DogStats dogStats;

    [Header("Turn Settings")]
    [SerializeField] private int maxTurns = 25;

    [Header("Result Panels")]
    [SerializeField] private GameObject playSuccessPanel;
    [SerializeField] private TMP_Text  playSuccessText;

    [SerializeField] private GameObject playFailPanel;
    [SerializeField] private TMP_Text  playFailText;

    [SerializeField] private GameObject restResultPanel;
    [SerializeField] private TMP_Text  restResultText;

    [SerializeField] private GameObject feedResultPanel;
    [SerializeField] private TMP_Text  feedResultText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Menu Reference")]
    [SerializeField] private MenuManager menuManager;


    public int CurrentTurn { get; private set; } = 0;
    public float CurrentFailChance { get; private set; } = 0f;

    private DogController currentDog;
    private bool isBusy;

    private ActionType pendingAction = ActionType.None;
    private Coroutine panelRoutine;

    // Track affection gain for Firebase logging
    private int lastAffectionGain = 0;

    private enum ActionType
    {
        None,
        PlaySuccess,
        PlayFail,
        Rest,
        Feed
    }

    private enum Zone
    {
        Low,
        Medium,
        High
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // ensure all panels are hidden at start
        HideAllResultPanels();

        RecalculateFailChance();
        UpdateButtonInteractable();

        // START NEW GAME SESSION
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.StartNewSession();
            Debug.Log("[DogManager] New Firebase session started");
        }
    }

    // Called by DogController.OnEnable
    public void RegisterDog(DogController dog)
    {
        currentDog = dog;
    }

    /// <summary>
    /// Called by DogController.StartAction(true) and OnActionAnimationEnd(false).
    /// </summary>
    public void SetBusy(bool busy)
    {
        isBusy = busy;
        UpdateButtonInteractable();

        // When animation finishes, resolve the pending action.
        if (!busy && pendingAction != ActionType.None)
        {
            ResolvePendingAction();
        }
    }

    private void UpdateButtonInteractable()
    {
        bool canUse = !isBusy && CurrentTurn < maxTurns;

        if (playButton != null) playButton.interactable = canUse;
        if (restButton != null) restButton.interactable = canUse;
        if (feedButton != null) feedButton.interactable = canUse;
    }

    /// <summary>
    /// Called by Back button on Game Over panel
    /// Returns to menu and resets game state
    /// </summary>
    public void OnBackToMenuButton()
    {
        // Hide game over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Reset game state for next play
        ResetGameState();
        
        // Show menu panel
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
    /// Reset all game variables for a new game
    /// </summary>
    private void ResetGameState()
    {
        CurrentTurn = 0;
        
        // Reset dog stats to starting values
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

    public void OnPlayButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns || dogStats == null)
            return;

        // Decide success or fail based on current fail chance
        float failChance = CurrentFailChance;
        float roll = Random.Range(0f, 100f);
        bool failed = roll < failChance;

        pendingAction = failed ? ActionType.PlayFail : ActionType.PlaySuccess;

        currentDog.Play(failed);
    }

    public void OnRestButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns) return;

        pendingAction = ActionType.Rest;
        currentDog.Rest();
    }

    public void OnFeedButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns) return;

        pendingAction = ActionType.Feed;
        currentDog.Feed();
    }

    // GAME LOGIC --------------------------------------------------------------

    private void ResolvePendingAction()
    {
        if (dogStats == null) return;

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

        pendingAction = ActionType.None;

        // One turn has been spent
        CurrentTurn++;
        LogCurrentTurnToFirebase();

        if (CurrentTurn >= maxTurns)
        {
            UpdateButtonInteractable(); // disable the buttons

            // END FIREBASE SESSION
            if (FirebaseDatabaseManager.Instance != null)
            {
                FirebaseDatabaseManager.Instance.EndSession(dogStats.affection);
                Debug.Log("[DogManager] Game ended. Session saved to Firebase");
            }

            // Show the Game Over panel
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            // Update score text
            if (finalScoreText != null)
            {
                string encouragement = GetEncouragementText(dogStats.affection);
                finalScoreText.text = $"Your Final Affection: {dogStats.affection}\n\n{encouragement}";
            }

            return;
        }

        // Recalculate fail chance for the next Play
        RecalculateFailChance();
    }

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

    private void ApplyPlaySuccess()
    {
        // SUCCESSFUL PLAY
        dogStats.energy -= 20f;
        dogStats.hunger -= 15f;

        // Random affection gain between 5 and 12 (inclusive)
        int affectionGain = Random.Range(5, 13);
        dogStats.affection += affectionGain;

        lastAffectionGain = affectionGain;

        ClampStats();

        if (playSuccessText != null)
        {
            playSuccessText.text = $"Your dog can’t stop wagging their tail! Affection +{affectionGain}";
        }

        ShowResultPanelForSeconds(playSuccessPanel);
    }

    private void ApplyPlayFail()
    {
        // FAILED PLAY
        dogStats.energy -= 10f;
        dogStats.hunger -= 10f;
        dogStats.affection -= 3;

        lastAffectionGain = -3;

        ClampStats();

        if (playFailText != null)
        {
            playFailText.text = "Your dog seems too tired or hungry to play right now. Affection -3";
        }

        ShowResultPanelForSeconds(playFailPanel);
    }

    private void ApplyRest()
    {
        dogStats.energy += 25f;
        dogStats.hunger -= 5f;

        lastAffectionGain = 0;

        ClampStats();

        if (restResultText != null)
        {
            restResultText.text = "Your dog curls up for a peaceful rest. Energy +25, Hunger −5.";
        }

        ShowResultPanelForSeconds(restResultPanel);
    }

    private void ApplyFeed()
    {
        dogStats.hunger += 35f;
        dogStats.energy += 5f;
        dogStats.affection += 2;

        lastAffectionGain = 2;

        ClampStats();

        if (feedResultText != null)
        {
            feedResultText.text = "Your dog enjoys a tasty meal. Hunger +35, Energy +5, \nAffection +5.";
        }

        ShowResultPanelForSeconds(feedResultPanel);
    }

    private void ClampStats()
    {
        dogStats.energy = Mathf.Clamp(dogStats.energy, 0f, 100f);
        dogStats.hunger = Mathf.Clamp(dogStats.hunger, 0f, 100f);

        if (dogStats.affection < 0)
            dogStats.affection = 0;
    }

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

    private Zone GetZone(float value)
    {
        if (value >= 70f) return Zone.High;
        if (value >= 31f) return Zone.Medium;
        return Zone.Low;
    }

    private float ComputeFailChance(float energy, float hunger)
    {
        // If dog is exhausted or starving, Play always fails
        if (energy <= 0f || hunger <= 0f)
            return 100f;

        // Perfect condition special case
        if (Mathf.Approximately(energy, 100f) && Mathf.Approximately(hunger, 100f))
            return 0f;

        Zone e = GetZone(energy);
        Zone h = GetZone(hunger);

        // Table from your design
        if (e == Zone.High && h == Zone.High) return 0f;
        if (e == Zone.High && h == Zone.Medium) return 20f;
        if (e == Zone.Medium && h == Zone.High) return 20f;
        if (e == Zone.Medium && h == Zone.Medium) return 40f;
        if (e == Zone.Low && h == Zone.High) return 70f;
        if (e == Zone.High && h == Zone.Low) return 70f;
        if (e == Zone.Medium && h == Zone.Low) return 80f;
        if (e == Zone.Low && h == Zone.Medium) return 80f;
        if (e == Zone.Low && h == Zone.Low) return 90f;

        // Fallback
        return 0f;
    }

    // RESULT PANEL HELPERS ----------------------------------------------------

    private void HideAllResultPanels()
    {
        if (playSuccessPanel != null) playSuccessPanel.SetActive(false);
        if (playFailPanel != null)    playFailPanel.SetActive(false);
        if (restResultPanel != null)  restResultPanel.SetActive(false);
        if (feedResultPanel != null)  feedResultPanel.SetActive(false);
    }

    private void ShowResultPanelForSeconds(GameObject panel)
    {
        if (panel == null) return;

        // stop any previous timer
        if (panelRoutine != null)
            StopCoroutine(panelRoutine);

        panelRoutine = StartCoroutine(ResultPanelRoutine(panel));
    }

    private IEnumerator ResultPanelRoutine(GameObject panel)
    {
        HideAllResultPanels();
        panel.SetActive(true);

        yield return new WaitForSeconds(3f);

        panel.SetActive(false);
        panelRoutine = null;
    }

    // FIREBASE INTEGRATION ----------------------------------------------------
    
    /// <summary>
    /// Log the current turn to Firebase with all relevant data
    /// </summary>
    private void LogCurrentTurnToFirebase()
    {
        if (FirebaseDatabaseManager.Instance == null || dogStats == null)
            return;

        // Determine action and result based on what just happened
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

        // Log turn to Firebase
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
