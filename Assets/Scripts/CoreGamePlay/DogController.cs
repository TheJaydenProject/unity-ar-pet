/// <summary>
/// Author: Jayden Wong
/// Date: 12 December 2025
/// Controls dog animations and coordinates with DogManager for gameplay flow.
/// Handles three main actions (Play, Rest, Feed) that lock UI buttons during
/// animation playback, plus a Shake interaction that doesn't lock buttons.
/// Uses Animator triggers to play animation clips and relies on Animation Events
/// at the end of clips to signal completion back to DogManager. Integrates with
/// AudioManager to play appropriate sound effects for each action.
/// </summary>

using UnityEngine;

public class DogController : MonoBehaviour
{
    // Reference to Animator component for triggering animation clips
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        // Register this dog with DogManager when enabled
        // Allows DogManager to track and control this dog instance
        DogManager.Instance?.RegisterDog(this);
    }

    /// <summary>
    /// Triggers play animation with success or fail variant.
    /// DogManager determines the outcome based on dog stats and passes result here.
    /// Plays appropriate sound effect and locks UI buttons until animation completes.
    /// </summary>
    public void Play(bool failed)
    {
        // Select animation trigger based on success/failure
        string trigger = failed ? "PlayFailTrigger" : "PlayTrigger";
        
        // Play corresponding sound effect
        if (AudioManager.Instance != null)
        {
            if (failed)
                AudioManager.Instance.PlayPlayFail();
            else
                AudioManager.Instance.PlayPlaySuccess();
        }
        
        // Start animation and lock buttons
        StartAction(trigger);
    }

    /// <summary>
    /// Triggers rest animation to restore dog's energy.
    /// Plays rest sound effect and locks UI buttons until animation completes.
    /// </summary>
    public void Rest()
    {
        // Play rest sound effect
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRest();
        
        // Start animation and lock buttons
        StartAction("RestTrigger");
    }

    /// <summary>
    /// Triggers feed animation to restore dog's hunger.
    /// Plays feed sound effect and locks UI buttons until animation completes.
    /// Currently uses same animation as Rest (placeholder).
    /// </summary>
    public void Feed()
    {
        // Play feed sound effect
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayFeed();
        
        // Start animation and lock buttons (uses Rest animation for now)
        StartAction("RestTrigger"); // same anim as Rest for now
    }

    /// <summary>
    /// Triggers shake/greeting animation when player taps on dog.
    /// This is a non-gameplay interaction that DOES NOT lock buttons,
    /// allowing player to tap the dog freely without interrupting gameplay.
    /// Called by DogTapHandler when player taps on the dog model.
    /// </summary>
    public void Shake()
    {
        // Play shake sound effect
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShake();
        
        // Trigger animation directly without locking buttons
        animator.SetTrigger("ShakeTrigger");
    }

    /// <summary>
    /// Starts an action animation and locks UI buttons.
    /// Used for gameplay actions (Play, Rest, Feed) that require
    /// waiting for animation completion before next action.
    /// </summary>
    private void StartAction(string trigger)
    {
        // Lock UI buttons to prevent actions during animation
        DogManager.Instance?.SetBusy(true);
        
        // Trigger the animation clip
        animator.SetTrigger(trigger);
    }

    /// <summary>
    /// Called by Animation Events at the end of Play/Rest/Feed animation clips.
    /// This is set up in the Animator timeline as an event callback.
    /// Re-enables UI buttons and signals DogManager to process action results.
    /// IMPORTANT: This method name must match the Animation Event exactly.
    /// </summary>
    public void OnActionAnimationEnd()
    {
        // Unlock UI buttons and let DogManager process the completed action
        DogManager.Instance?.SetBusy(false);
    }
}