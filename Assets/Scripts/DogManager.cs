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

    [Header("Feedback UI (optional)")]
    [SerializeField] private TMP_Text resultText;   // e.g. "Play failed!" / "Play success!"

    public int CurrentTurn { get; private set; } = 0;
    public float CurrentFailChance { get; private set; } = 0f;

    private DogController currentDog;
    private bool isBusy;

    private ActionType pendingAction = ActionType.None;

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
        RecalculateFailChance();
        UpdateButtonInteractable();

        if (resultText != null)
            resultText.text = "";
    }

    // Called by DogController.OnEnable
    public void RegisterDog(DogController dog)
    {
        currentDog = dog;
    }

    /// <summary>
    /// Called by DogController.StartAction (true) and OnActionAnimationEnd (false).
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

    // UI BUTTON HANDLERS  -----------------------------------------------------

    public void OnPlayButton()
    {
        if (isBusy || currentDog == null || CurrentTurn >= maxTurns || dogStats == null)
            return;

        // Use the CURRENT fail chance to decide outcome
        float failChance = CurrentFailChance;
        float roll = Random.Range(0f, 100f);
        bool failed = roll < failChance;

        pendingAction = failed ? ActionType.PlayFail : ActionType.PlaySuccess;

        // Trigger correct animation (result text is set AFTER stats update)
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

        if (CurrentTurn >= maxTurns)
        {
            // Out of turns → disable input; show final result
            UpdateButtonInteractable();

            if (resultText != null)
            {
                resultText.color = Color.white;
                resultText.text = $"Game Over! Final Affection: {dogStats.affection}";
            }
        }

        // Recalculate fail chance for the next Play
        RecalculateFailChance();
    }

    private void ApplyPlaySuccess()
    {
        // SUCCESSFUL PLAY
        dogStats.energy -= 20f;
        dogStats.hunger -= 15f;

        // Random affection gain between 1 and 12 (inclusive)
        int affectionGain = Random.Range(1, 13);
        dogStats.affection += affectionGain;

        ClampStats();

        if (resultText != null)
        {
            resultText.color = Color.white;
            resultText.text = $"Play success! Gained {affectionGain} affection.";
        }
    }

    private void ApplyPlayFail()
    {
        // FAILED PLAY
        dogStats.energy -= 10f;
        dogStats.hunger -= 10f;

        ClampStats();

        if (resultText != null)
        {
            resultText.color = Color.red;
            resultText.text = "Play failed! Dog was too tired or hungry.";
        }
    }

    private void ApplyRest()
    {
        dogStats.energy += 25f;
        dogStats.hunger -= 5f;
        dogStats.affection += 5;

        ClampStats();

        if (resultText != null)
        {
            resultText.color = Color.white;
            resultText.text = "Dog had a wonderful rest. Energy +25, Hunger -5.";
        }
    }

    private void ApplyFeed()
    {
        dogStats.hunger += 35f;
        dogStats.energy += 5f;
        dogStats.affection += 5;

        ClampStats();

        if (resultText != null)
        {
            resultText.color = Color.white;
            resultText.text = "Dog enjoyed a tasty snack. Hunger +35, Energy +5.";
        }
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
}
