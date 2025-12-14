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
        
        // Play appropriate sound
        if (AudioManager.Instance != null)
        {
            if (failed)
                AudioManager.Instance.PlayPlayFail();
            else
                AudioManager.Instance.PlayPlaySuccess();
        }
        
        StartAction(trigger);
    }

    public void Rest()
    {
        // Play rest sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRest();
            
        StartAction("RestTrigger");
    }

    public void Feed()
    {
        // Play feed sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayFeed();
            
        StartAction("RestTrigger"); // same anim as Rest for now
    }

    // Tap-on-dog interaction: DOES NOT lock buttons
    public void Shake()
    {
        // Play shake sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShake();
            
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