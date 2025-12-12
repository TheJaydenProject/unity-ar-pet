using System;
using System.Collections.Generic;

[Serializable]
public class GameSession
{
    public string sessionId;
    public long startTime;
    public long endTime;
    public int finalAffection;
    public List<TurnLog> turns;
    
    public GameSession()
    {
        sessionId = Guid.NewGuid().ToString();
        startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        endTime = 0;
        finalAffection = 0;
        turns = new List<TurnLog>();
    }
}