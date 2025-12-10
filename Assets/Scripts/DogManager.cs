using UnityEngine;
using UnityEngine.UI;

public class DogManager : MonoBehaviour
{
    public static DogManager Instance { get; private set; }

    [Header("UI Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button restButton;
    [SerializeField] private Button feedButton;

    private DogController currentDog;
    private bool isBusy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterDog(DogController dog)
    {
        currentDog = dog;
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;

        playButton.interactable = !busy;
        restButton.interactable = !busy;
        feedButton.interactable = !busy;
    }

    public void OnPlayButton()
    {
        if (isBusy || currentDog == null) return;
        currentDog.Play();
    }

    public void OnRestButton()
    {
        if (isBusy || currentDog == null) return;
        currentDog.Rest();
    }

    public void OnFeedButton()
    {
        if (isBusy || currentDog == null) return;
        currentDog.Feed();
    }
}
