/// <summary>
/// Author: Jayden Wong
/// Date: 11 December 2025
/// Real-time UI display system for dog stats and game metrics.
/// Continuously syncs UI elements (sliders, text labels) with DogStats values
/// every frame to provide immediate visual feedback. Displays energy and hunger
/// as both sliders (visual bars) and text (numeric values), shows affection
/// as the player's score, and displays the current play action fail chance
/// percentage from DogManager. Ensures slider ranges match stat ranges (0-100).
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("Data")]
    // Reference to the dog's stat component for reading current values
    [SerializeField] private DogStats dogStats;

    [Header("Sliders")]
    // Visual bar showing energy level (0-100 range)
    [SerializeField] private Slider energySlider;
    
    // Visual bar showing hunger level (0-100 range)
    [SerializeField] private Slider hungerSlider;

    [Header("Texts")]
    // Numeric display for energy value (format: "80/100")
    [SerializeField] private TMP_Text energyText;
    
    // Numeric display for hunger value (format: "80/100")
    [SerializeField] private TMP_Text hungerText;
    
    // Display for affection score (player's total points)
    [SerializeField] private TMP_Text affectionText;
    
    // Display for current play action fail chance percentage
    [SerializeField] private TMP_Text failChanceText;


    private void Start()
    {
        // Configure slider ranges to match stat ranges (0-100)
        // This ensures slider visual accurately represents stat values
        if (energySlider != null)
        {
            energySlider.minValue = 0f;
            energySlider.maxValue = 100f;
        }

        if (hungerSlider != null)
        {
            hungerSlider.minValue = 0f;
            hungerSlider.maxValue = 100f;
        }
    }

    private void Update()
    {
        // Skip update if DogStats reference is missing
        if (dogStats == null) return;

        // Update slider fill amounts to match current stat values
        if (energySlider != null)
            energySlider.value = dogStats.energy;

        if (hungerSlider != null)
            hungerSlider.value = dogStats.hunger;

        // Update text labels with formatted stat values
        // Format: "80/100" - shows current value out of maximum
        if (energyText != null)
            energyText.text = $"{dogStats.energy:0}/100";

        if (hungerText != null)
            hungerText.text = $"{dogStats.hunger:0}/100";

        // Display affection score as integer (player's total points)
        if (affectionText != null)
            affectionText.text = dogStats.affection.ToString();
        
        // Display current play fail chance percentage from DogManager
        // Format: "25%" - shows probability of play action failing
        if (failChanceText != null && DogManager.Instance != null)
        {
            failChanceText.text = $"{DogManager.Instance.CurrentFailChance:0}%";
        }
    }
}