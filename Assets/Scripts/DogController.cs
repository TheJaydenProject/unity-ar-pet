using UnityEngine;

public class DogController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        DogManager.Instance?.RegisterDog(this);
    }

    public void Play()
    {
        StartAction("PlayTrigger");
    }

    public void Rest()
    {
        StartAction("RestTrigger");
    }

    public void Feed()
    {
        StartAction("RestTrigger"); // same anim as Rest for now
    }

    private void StartAction(string trigger)
    {
        DogManager.Instance?.SetBusy(true);   // disable buttons
        animator.SetTrigger(trigger);
    }

    // THIS will be called by Animation Events at the end of action clips
    public void OnActionAnimationEnd()
    {
        DogManager.Instance?.SetBusy(false);  // re-enable buttons
    }
}
