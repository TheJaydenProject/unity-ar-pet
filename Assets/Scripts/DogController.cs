using UnityEngine;

public class DogController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        DogManager.Instance?.RegisterDog(this);
    }

    // Play with outcome decided by DogManager
    public void Play(bool failed)
    {
        string trigger = failed ? "PlayFailTrigger" : "PlayTrigger";
        StartAction(trigger);
    }

    public void Rest()
    {
        StartAction("RestTrigger");
    }

    public void Feed()
    {
        StartAction("RestTrigger"); // same anim as Rest for now
    }

    // Tap-on-dog interaction: DOES NOT lock buttons
    public void Shake()
    {
        animator.SetTrigger("ShakeTrigger");
    }

    private void StartAction(string trigger)
    {
        DogManager.Instance?.SetBusy(true);   // disable buttons
        animator.SetTrigger(trigger);
    }

    // Called by Animation Events at the end of Play/Rest/Feed clips
    public void OnActionAnimationEnd()
    {
        DogManager.Instance?.SetBusy(false);  // re-enable buttons and resolve action
    }
}
