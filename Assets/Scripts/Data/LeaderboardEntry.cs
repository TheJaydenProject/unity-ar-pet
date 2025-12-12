using System;

[Serializable]
public class LeaderboardEntry
{
    public string userId;
    public string displayName;
    public int highestAffection;
    public long lastUpdated;
    
    public LeaderboardEntry(string uid, string name, int score)
    {
        userId = uid;
        displayName = name;
        highestAffection = score;
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}