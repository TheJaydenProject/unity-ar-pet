/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Represents a single entry in the global leaderboard.
/// Stores user identification, display name, their best score, and when
/// the entry was last updated. Includes both a parameterless constructor
/// (required for Unity's JsonUtility deserialization from Firebase) and
/// a parameterized constructor for creating new entries with data.
/// Marked [Serializable] for Firebase JSON serialization.
/// </summary>

using System;

[Serializable]
public class LeaderboardEntry
{
    // Firebase Auth UID of the user who owns this entry
    public string userId;
    
    // Display name shown on leaderboard
    public string displayName;
    
    // User's highest affection score across all sessions
    public int highestAffection;
    
    // Unix timestamp when this entry was last updated
    public long lastUpdated;
    
    /// <summary>
    /// Parameterless constructor required for JsonUtility deserialization.
    /// Unity's JsonUtility needs this when reading data from Firebase.
    /// Initializes all fields to safe default values.
    /// </summary>
    public LeaderboardEntry()
    {
        userId = "";
        displayName = "";
        highestAffection = 0;
        lastUpdated = 0;
    }
    
    /// <summary>
    /// Creates a new leaderboard entry with provided data.
    /// Automatically sets lastUpdated to current timestamp.
    /// Use this when creating or updating leaderboard entries.
    /// </summary>
    public LeaderboardEntry(string uid, string name, int score)
    {
        userId = uid;
        displayName = name;
        highestAffection = score;
        
        // Record when this entry was created/updated
        lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}