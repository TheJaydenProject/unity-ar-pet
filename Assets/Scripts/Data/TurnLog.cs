using System;

[Serializable]
public class TurnLog
{
    public int turnNumber;
    public string action;
    public string result;
    public int affectionGained;
    public float energyAfter;
    public float hungerAfter;
    public long timestamp;
    
    public TurnLog(int turn, string act, string res, int affGain, float energy, float hunger)
    {
        turnNumber = turn;
        action = act;
        result = res;
        affectionGained = affGain;
        energyAfter = energy;
        hungerAfter = hunger;
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}