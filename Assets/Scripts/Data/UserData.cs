/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Represents a user's profile data stored in Firebase Realtime Database.
/// Contains authentication info, account creation timestamp, and gameplay
/// statistics (total games played, personal best score). The userId field
/// matches the Firebase Auth UID for consistent cross-reference between
/// authentication and database systems. Password is stored in plain text
/// for demo password reset functionality (not recommended for production).
/// Marked [Serializable] for Unity's JsonUtility serialization to Firebase.
/// </summary>

using System;

[Serializable]
public class UserData
{
    // Firebase Auth UID - primary key linking auth account to database profile
    public string userId;
    
    // User's email address used for authentication
    public string email;
    
    // Plain text password stored for demo password reset (NOT production-safe)
    public string password;
    
    // Display name shown on leaderboard and throughout the game
    public string displayName;
    
    // Unix timestamp when user account was created
    public long createdAt;
    
    // Career statistic: total number of game sessions completed
    public int totalGamesPlayed;
    
    // Personal best: highest affection score ever achieved by this user
    public int highestAffection;
    
    /// <summary>
    /// Creates a new UserData object with default values.
    /// Initializes createdAt to current timestamp and zeroes out stats.
    /// Used by FirebaseAuthManager when creating new user profiles.
    /// </summary>
    public UserData()
    {
        // Record account creation time
        createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Initialize gameplay statistics at zero
        totalGamesPlayed = 0;
        highestAffection = 0;
    }
}