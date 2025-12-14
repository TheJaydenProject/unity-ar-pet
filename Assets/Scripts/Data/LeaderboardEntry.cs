using System;

[Serializable]
public class LeaderboardEntry
{
    public string userId;
    public string displayName;
    public int highestAffection;
    public long lastUpdated;
    
    // Parameterless constructor required for JsonUtility deserialization
    public LeaderboardEntry()
    {
        userId = "";
        displayName = "";
        highestAffection = 0;
        lastUpdated = 0;
    }
    
    // Constructor with parameters for creating new entries
    public LeaderboardEntry(string uid, string name, int score)
    {
        userId = uid;
        displayName = name;
        highestAffection = score;
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}