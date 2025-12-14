/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Records detailed data for a single turn within a game session.
/// Captures player action, outcome, affection gained, and character state
/// (energy/hunger levels) after the turn completes. Includes automatic
/// timestamping for turn-by-turn analytics. Used to build complete
/// gameplay history for each session stored in Firebase.
/// Marked [Serializable] for Unity's JsonUtility serialization.
/// </summary>

using System;

[Serializable]
public class TurnLog
{
    // Sequential turn number in the session (1-25 typically)
    public int turnNumber;
    
    // Player action taken (e.g., "Play", "Rest", "Feed")
    public string action;
    
    // Outcome of the action (e.g., "Success", "Failure", descriptive text)
    public string result;
    
    // Amount of affection gained (or lost if negative) from this turn
    public int affectionGained;
    
    // Character's energy level after this turn was processed
    public float energyAfter;
    
    // Character's hunger level after this turn was processed
    public float hungerAfter;
    
    // Unix timestamp when this turn occurred
    public long timestamp;
    
    /// <summary>
    /// Creates a new turn log with all relevant gameplay data.
    /// Automatically captures current timestamp for turn timing analytics.
    /// Called by FirebaseDatabaseManager.LogTurn() after each player action.
    /// </summary>
    public TurnLog(int turn, string act, string res, int affGain, float energy, float hunger)
    {
        turnNumber = turn;
        action = act;
        result = res;
        affectionGained = affGain;
        energyAfter = energy;
        hungerAfter = hunger;
        
        // Record exact moment this turn was logged
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}