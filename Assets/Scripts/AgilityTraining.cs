using UnityEngine;
using UnityEngine.UI;

public class AgilityTraining : MonoBehaviour
{
    public PetStatsHandler pet;
    public Button targetButton;

    private float timer;

    void OnEnable()
    {
        timer = 3f; // 3-second session
        SpawnNewTarget();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
            EndTraining();
    }

    public void OnTargetHit()
    {
        pet.IncreaseStat("agility", 1);
        SpawnNewTarget();
    }

    void SpawnNewTarget()
    {
        RectTransform rt = targetButton.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(
            Random.Range(-300, 300),
            Random.Range(-600, 600)
        );
    }

    void EndTraining()
    {
        this.gameObject.SetActive(false);
    }
}
