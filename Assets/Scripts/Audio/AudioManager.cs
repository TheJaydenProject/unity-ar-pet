/// <summary>
/// Author: Jayden Wong
/// Date: December 2025
/// Centralized audio management system for the dog game.
/// Uses singleton pattern to provide global access to sound playback.
/// Separates audio into two channels: dog action sounds and UI sounds,
/// each with independent volume control. Uses PlayOneShot for overlapping
/// sound effects without interrupting previous plays. Persists across
/// scene loads to maintain audio state throughout the game.
/// </summary>

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    // Dedicated audio source for dog-related sounds (play, rest, feed, etc.)
    [SerializeField] private AudioSource dogSoundSource;
    
    // Dedicated audio source for UI interaction sounds (buttons, game over)
    [SerializeField] private AudioSource uiSoundSource;

    [Header("Dog Action Sounds")]
    [SerializeField] private AudioClip playSuccessSound;
    [SerializeField] private AudioClip playFailSound;
    [SerializeField] private AudioClip restSound;
    [SerializeField] private AudioClip feedSound;
    [SerializeField] private AudioClip shakeSound;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip gameOverSound;

    [Header("Volume Settings")]
    // Master volume for all dog action sounds (0 = silent, 1 = full volume)
    [Range(0f, 1f)] [SerializeField] private float dogSoundVolume = 0.7f;
    
    // Master volume for all UI sounds (0 = silent, 1 = full volume)
    [Range(0f, 1f)] [SerializeField] private float uiSoundVolume = 0.5f;

    private void Awake()
    {
        // Singleton pattern - ensure only one AudioManager exists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Persist this GameObject across scene loads
        DontDestroyOnLoad(gameObject);

        // Apply initial volume settings to audio sources
        if (dogSoundSource != null) dogSoundSource.volume = dogSoundVolume;
        if (uiSoundSource != null) uiSoundSource.volume = uiSoundVolume;
    }

    // ========== DOG ACTION SOUNDS ==========

    /// <summary>
    /// Plays sound effect for successful play action.
    /// Called when player successfully plays with the dog.
    /// </summary>
    public void PlayPlaySuccess()
    {
        PlaySound(dogSoundSource, playSuccessSound);
    }

    /// <summary>
    /// Plays sound effect for failed play action.
    /// Called when play action fails (e.g., dog too tired).
    /// </summary>
    public void PlayPlayFail()
    {
        PlaySound(dogSoundSource, playFailSound);
    }

    /// <summary>
    /// Plays sound effect for rest action.
    /// Called when player chooses to let the dog rest.
    /// </summary>
    public void PlayRest()
    {
        PlaySound(dogSoundSource, restSound);
    }

    /// <summary>
    /// Plays sound effect for feed action.
    /// Called when player feeds the dog.
    /// </summary>
    public void PlayFeed()
    {
        PlaySound(dogSoundSource, feedSound);
    }

    /// <summary>
    /// Plays sound effect for shake/greeting action.
    /// Called when dog shakes or greets the player.
    /// </summary>
    public void PlayShake()
    {
        PlaySound(dogSoundSource, shakeSound);
    }

    // ========== UI SOUNDS ==========

    /// <summary>
    /// Plays sound effect for button clicks.
    /// Called by UI buttons throughout the game for consistent feedback.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySound(uiSoundSource, buttonClickSound);
    }

    /// <summary>
    /// Plays sound effect when game ends.
    /// Called when the 25-turn session completes.
    /// </summary>
    public void PlayGameOver()
    {
        PlaySound(uiSoundSource, gameOverSound);
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Core sound playback method using PlayOneShot.
    /// PlayOneShot allows multiple sounds to overlap without interrupting
    /// each other, unlike Play() which stops the previous sound.
    /// Validates source and clip exist before playing to avoid null errors.
    /// </summary>
    private void PlaySound(AudioSource source, AudioClip clip)
    {
        // Safety check: only play if both source and clip are assigned
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    // ========== VOLUME CONTROLS ==========

    /// <summary>
    /// Adjusts the master volume for all dog action sounds.
    /// Clamps value between 0 and 1 to prevent invalid volumes.
    /// Useful for settings menus or dynamic volume adjustments.
    /// </summary>
    public void SetDogSoundVolume(float volume)
    {
        // Clamp to valid range (0-1)
        dogSoundVolume = Mathf.Clamp01(volume);
        
        // Apply to audio source immediately
        if (dogSoundSource != null)
            dogSoundSource.volume = dogSoundVolume;
    }

    /// <summary>
    /// Adjusts the master volume for all UI sounds.
    /// Clamps value between 0 and 1 to prevent invalid volumes.
    /// Useful for settings menus or dynamic volume adjustments.
    /// </summary>
    public void SetUISoundVolume(float volume)
    {
        // Clamp to valid range (0-1)
        uiSoundVolume = Mathf.Clamp01(volume);
        
        // Apply to audio source immediately
        if (uiSoundSource != null)
            uiSoundSource.volume = uiSoundVolume;
    }

    /// <summary>
    /// Mutes or unmutes all audio sources simultaneously.
    /// Useful for quick mute toggle in settings or pause menus.
    /// Does not affect volume levels - can be unmuted to restore sound.
    /// </summary>
    public void MuteAll(bool mute)
    {
        if (dogSoundSource != null) dogSoundSource.mute = mute;
        if (uiSoundSource != null) uiSoundSource.mute = mute;
    }
}