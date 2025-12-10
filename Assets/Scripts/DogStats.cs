using UnityEngine;

public class DogStats : MonoBehaviour
{
    // 0–100
    [Range(0, 100)] public float energy = 80f;

    // 0 = starving, 100 = full
    [Range(0, 100)] public float hunger = 80f;

    // happiness / score
    public int affection = 10;
}
