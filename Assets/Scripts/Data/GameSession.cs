/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Represents a single game session with all associated turn data.
/// Automatically generates a unique session ID using GUID and captures
/// start/end timestamps in Unix epoch format for consistent cross-platform
/// time tracking. Stores a complete history of all turns played during the session.
/// Marked [Serializable] for Unity's JsonUtility serialization to Firebase.
/// </summary>

using System;
using System.Collections.Generic;

[Serializable]
public class GameSession
{
    // Unique identifier for this game session (auto-generated GUID)
    public string sessionId;
    
    // Unix timestamp (seconds since epoch) when session started
    public long startTime;
    
    // Unix timestamp when session ended (0 until EndSession is called)
    public long endTime;
    
    // Final affection score achieved at end of session
    public int finalAffection;
    
    // Complete list of all turns played during this session
    public List<TurnLog> turns;
    
    /// <summary>
    /// Creates a new game session with auto-generated ID and current timestamp.
    /// Initializes empty turn list ready to record gameplay.
    /// </summary>
    public GameSession()
    {
        // Generate unique session ID for database storage
        sessionId = Guid.NewGuid().ToString();
        
        // Record session start time in Unix epoch format
        startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Initialize end state (will be set when session completes)
        endTime = 0;
        finalAffection = 0;
        
        // Prepare list to store all turn logs
        turns = new List<TurnLog>();
    }
}