using UnityEngine;

[System.Serializable]
public class PetStats
{
    public int level = 1;
    public int agility = 0;
    public int xp = 0;
}

public class PetStatsHandler : MonoBehaviour
{
    public PetStats stats = new PetStats();

    public void AddXP(int amount)
    {
        stats.xp += amount;
        if (stats.xp >= stats.level * 100)
        {
            stats.xp = 0;
            stats.level++;
        }
    }

    public void IncreaseStat(string statName, int amount)
    {
        switch (statName)
        {
            case "agility": stats.agility += amount; break;
        }
    }
}
