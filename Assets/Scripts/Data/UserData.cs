using System;

[Serializable]
public class UserData
{
    public string userId;
    public string email;
    public string displayName;
    public long createdAt;
    public int totalGamesPlayed;
    public int highestAffection;
    
    public UserData()
    {
        createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        totalGamesPlayed = 0;
        highestAffection = 0;
    }
}