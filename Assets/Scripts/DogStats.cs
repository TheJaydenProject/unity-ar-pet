// tracks dog's stats (Strength, Affection and Health)
using UnityEngine;

public class DogStats : MonoBehaviour
{
    public float strength = 1f;
    public float affection = 1f;
    public float health = 100f;

    public float healthDecreaseRate = 2f; // health per minute

    void Update()
    {
        health -= (healthDecreaseRate / 60f) * Time.deltaTime;

        health = Mathf.Clamp(health, 0, 100);
    }

    public void Train()
    {
        strength += 1f;
    }

    public void Roll()
    {
        affection += 2f;
        affection = Mathf.Clamp(affection, 0, 100);
    }

    public void Rest()
    {
        health += 10f;
        health = Mathf.Clamp(health, 0, 100);
    }
}
