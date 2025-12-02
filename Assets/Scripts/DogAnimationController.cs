using UnityEngine;

public class DogAnimationController : MonoBehaviour
{
    private Animator anim;
    private DogStats stats;

    void Start()
    {
        anim = GetComponent<Animator>();
        stats = GetComponent<DogStats>(); 
    }

    public void PlayRollOver() // Train
    {
        anim.SetTrigger("RollOver");
        stats.strength += 1;
    }

    public void PlayShake() // Play
    {
        anim.SetTrigger("Shake");
        stats.affection += 2;
    }

    public void PlaySit() // Relax
    {
        anim.SetTrigger("Sit");
        stats.health += 5;
        stats.health = Mathf.Clamp(stats.health, 0, 100);
    }
}
