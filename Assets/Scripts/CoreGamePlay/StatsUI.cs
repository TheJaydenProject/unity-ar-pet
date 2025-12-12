using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DogStats dogStats;

    [Header("Sliders")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider hungerSlider;

    [Header("Texts")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text hungerText;
    [SerializeField] private TMP_Text affectionText;
    [SerializeField] private TMP_Text failChanceText;


    private void Start()
    {
        // ensure slider ranges are 0–100
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
        if (dogStats == null) return;

        // update sliders
        if (energySlider != null)
            energySlider.value = dogStats.energy;

        if (hungerSlider != null)
            hungerSlider.value = dogStats.hunger;

        // update text labels
        if (energyText != null)
            energyText.text = $"{dogStats.energy:0}/100";

        if (hungerText != null)
            hungerText.text = $"{dogStats.hunger:0}/100";

        if (affectionText != null)
            affectionText.text = dogStats.affection.ToString();
            
        if (failChanceText != null && DogManager.Instance != null)
        {
            failChanceText.text = $"{DogManager.Instance.CurrentFailChance:0}%";
        }
    }
}
