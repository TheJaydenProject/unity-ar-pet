/// <summary>
/// 
/// Author: Your Name
/// Date: December 2025
/// Purpose:
/// Centralized audio management system for the dog game.
/// Handles all sound effects (no background music or feedback chimes).
/// 
/// </summary>

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource dogSoundSource;
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
    [Range(0f, 1f)] [SerializeField] private float dogSoundVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float uiSoundVolume = 0.5f;

    private void Awake()
    {
        // Singleton pattern - only one AudioManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Set initial volumes
        if (dogSoundSource != null) dogSoundSource.volume = dogSoundVolume;
        if (uiSoundSource != null) uiSoundSource.volume = uiSoundVolume;
    }

    // ========== DOG ACTION SOUNDS ==========

    public void PlayPlaySuccess()
    {
        PlaySound(dogSoundSource, playSuccessSound);
    }

    public void PlayPlayFail()
    {
        PlaySound(dogSoundSource, playFailSound);
    }

    public void PlayRest()
    {
        PlaySound(dogSoundSource, restSound);
    }

    public void PlayFeed()
    {
        PlaySound(dogSoundSource, feedSound);
    }

    public void PlayShake()
    {
        PlaySound(dogSoundSource, shakeSound);
    }

    // ========== UI SOUNDS ==========

    public void PlayButtonClick()
    {
        PlaySound(uiSoundSource, buttonClickSound);
    }

    public void PlayGameOver()
    {
        PlaySound(uiSoundSource, gameOverSound);
    }

    // ========== HELPER METHODS ==========

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    // ========== VOLUME CONTROLS ==========

    public void SetDogSoundVolume(float volume)
    {
        dogSoundVolume = Mathf.Clamp01(volume);
        if (dogSoundSource != null)
            dogSoundSource.volume = dogSoundVolume;
    }

    public void SetUISoundVolume(float volume)
    {
        uiSoundVolume = Mathf.Clamp01(volume);
        if (uiSoundSource != null)
            uiSoundSource.volume = uiSoundVolume;
    }

    public void MuteAll(bool mute)
    {
        if (dogSoundSource != null) dogSoundSource.mute = mute;
        if (uiSoundSource != null) uiSoundSource.mute = mute;
    }
}