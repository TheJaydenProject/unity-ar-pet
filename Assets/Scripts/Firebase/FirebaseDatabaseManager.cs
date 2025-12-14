/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Manages all Firebase Realtime Database operations for the game.
/// Handles game session tracking (start, turn logging, end), user statistics
/// updates (games played, high scores), and leaderboard management.
/// Uses Firebase's async pattern with ContinueWithOnMainThread for safe
/// Unity main thread execution of all database callbacks.
/// </summary>

using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance { get; private set; }
    
    // Current active game session data
    private GameSession currentSession;
    private string currentUserId;
    
    // Events to notify UI of database operations
    public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
    public event Action<string> OnDatabaseError;
    
    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Initializes a new game session for the authenticated user.
    /// Creates a fresh GameSession object to track turns and outcomes.
    /// Should be called when starting a new game.
    /// </summary>
    public void StartNewSession()
    {
        if (!FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[DatabaseManager] Cannot start session - user not authenticated");
            return;
        }
        
        // Store user ID and create new session object
        currentUserId = FirebaseManager.Instance.CurrentUserId;
        currentSession = new GameSession();
        
        Debug.Log("[DatabaseManager] New session started: " + currentSession.sessionId);
    }
    
    /// <summary>
    /// Records a single turn in the current game session.
    /// Captures player action, result, and character state for analytics.
    /// If no session exists, automatically creates one.
    /// </summary>
    public void LogTurn(int turnNumber, string action, string result, int affectionGained, float energy, float hunger)
    {
        // Auto-create session if none exists
        if (currentSession == null)
        {
            Debug.LogWarning("[DatabaseManager] No active session. Starting new session...");
            StartNewSession();
        }
        
        // Create turn log entry and add to current session
        TurnLog turn = new TurnLog(turnNumber, action, result, affectionGained, energy, hunger);
        currentSession.turns.Add(turn);
        
        Debug.Log("[DatabaseManager] Turn " + turnNumber + " logged: " + action + " - " + result);
    }
    
    /// <summary>
    /// Finalizes the current game session and persists all data to Firebase.
    /// Performs three operations:
    /// 1. Saves complete session data with all turns
    /// 2. Updates user profile statistics
    /// 3. Updates leaderboard if score qualifies
    /// Should be called when game ends (typically after 25 turns).
    /// </summary>
    public void EndSession(int finalAffection)
    {
        if (currentSession == null)
        {
            Debug.LogError("[DatabaseManager] No active session to end");
            return;
        }
        
        if (!FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[DatabaseManager] Cannot end session - user not authenticated");
            return;
        }
        
        // Record end time and final score
        currentSession.endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        currentSession.finalAffection = finalAffection;
        
        Debug.Log("[DatabaseManager] Ending session. Final affection: " + finalAffection);
        
        // Trigger all save operations
        SaveSessionToDatabase();
        UpdateUserStats(finalAffection);
        UpdateLeaderboardIfNeeded(finalAffection);
    }
    
    /// <summary>
    /// Persists the current session data to Firebase.
    /// Saves to path: users/{userId}/sessions/{sessionId}
    /// Includes all turn logs and session metadata.
    /// </summary>
    private void SaveSessionToDatabase()
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        
        // Serialize session data to JSON
        string json = JsonUtility.ToJson(currentSession);
        
        // Construct database path for this session
        string path = FirebaseConfig.USERS_PATH + "/" + currentUserId + "/" + 
                     FirebaseConfig.SESSIONS_PATH + "/" + currentSession.sessionId;
        
        // Write session data to database
        dbRef.Child(path).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[DatabaseManager] Failed to save session: " + task.Exception);
                OnDatabaseError?.Invoke("Failed to save game session");
                return;
            }
            
            Debug.Log("[DatabaseManager] Session saved successfully: " + path);
        });
    }
    
    /// <summary>
    /// Updates user profile statistics after game completion.
    /// Increments totalGamesPlayed and updates highestAffection if new record.
    /// Reads existing profile, modifies fields, then writes back.
    /// </summary>
    private void UpdateUserStats(int finalAffection)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string profilePath = FirebaseConfig.USERS_PATH + "/" + currentUserId + "/" + FirebaseConfig.PROFILE_PATH;
        
        // Read current profile from database
        dbRef.Child(profilePath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[DatabaseManager] Failed to read user profile: " + task.Exception);
                return;
            }
            
            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogError("[DatabaseManager] User profile not found");
                return;
            }
            
            // Parse existing profile data
            string json = snapshot.GetRawJsonValue();
            UserData userData = JsonUtility.FromJson<UserData>(json);
            
            // Update statistics
            userData.totalGamesPlayed++;
            if (finalAffection > userData.highestAffection)
            {
                userData.highestAffection = finalAffection;
                Debug.Log("[DatabaseManager] New high score: " + finalAffection);
            }
            
            // Write updated profile back to database
            string updatedJson = JsonUtility.ToJson(userData);
            dbRef.Child(profilePath).SetRawJsonValueAsync(updatedJson).ContinueWithOnMainThread(writeTask =>
            {
                if (writeTask.IsFaulted || writeTask.IsCanceled)
                {
                    Debug.LogError("[DatabaseManager] Failed to update user stats: " + writeTask.Exception);
                    return;
                }
                
                Debug.Log("[DatabaseManager] User stats updated. Total games: " + userData.totalGamesPlayed);
            });
        });
    }
    
    /// <summary>
    /// Updates leaderboard entry if final score exceeds user's previous best.
    /// Creates new entry if user not on leaderboard yet.
    /// Only updates if score is higher than existing entry.
    /// </summary>
    private void UpdateLeaderboardIfNeeded(int finalAffection)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string leaderboardPath = FirebaseConfig.LEADERBOARD_PATH + "/" + currentUserId;
        
        // Get current user's display name for leaderboard entry
        FirebaseUser user = FirebaseManager.Instance.CurrentUser;
        string displayName = user.DisplayName ?? user.Email;
        
        // Check if user already has a leaderboard entry
        dbRef.Child(leaderboardPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[DatabaseManager] Failed to read leaderboard entry: " + task.Exception);
                return;
            }
            
            DataSnapshot snapshot = task.Result;
            bool shouldUpdate = false;
            
            if (!snapshot.Exists)
            {
                // No existing entry - this is user's first qualifying score
                shouldUpdate = true;
            }
            else
            {
                // Entry exists - check if new score beats old one
                string json = snapshot.GetRawJsonValue();
                LeaderboardEntry existingEntry = JsonUtility.FromJson<LeaderboardEntry>(json);
                
                if (finalAffection > existingEntry.highestAffection)
                {
                    shouldUpdate = true;
                }
            }
            
            // Only write to database if score qualifies
            if (shouldUpdate)
            {
                // Create new leaderboard entry with current score
                LeaderboardEntry newEntry = new LeaderboardEntry(currentUserId, displayName, finalAffection);
                string entryJson = JsonUtility.ToJson(newEntry);
                
                // Write to leaderboard path
                dbRef.Child(leaderboardPath).SetRawJsonValueAsync(entryJson).ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsFaulted || writeTask.IsCanceled)
                    {
                        Debug.LogError("[DatabaseManager] Failed to update leaderboard: " + writeTask.Exception);
                        return;
                    }
                    
                    Debug.Log("[DatabaseManager] Leaderboard updated with score: " + finalAffection);
                });
            }
        });
    }
    
    /// <summary>
    /// Fetches top leaderboard entries ordered by highest affection score.
    /// Uses Firebase's OrderByChild and LimitToLast for efficient querying.
    /// Results are sorted descending and returned via OnLeaderboardLoaded event.
    /// </summary>
    public void FetchLeaderboard(int limit = 10)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string leaderboardPath = FirebaseConfig.LEADERBOARD_PATH;
        
        Debug.Log("[DatabaseManager] Starting FetchLeaderboard for path: " + leaderboardPath);
        
        // Disable keep synced to avoid unnecessary background syncing
        dbRef.Child(leaderboardPath).KeepSynced(false);
        
        // Query database: order by highestAffection, get top N entries
        dbRef.Child(leaderboardPath)
            .OrderByChild("highestAffection")
            .LimitToLast(limit)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[DatabaseManager] Failed to fetch leaderboard: " + task.Exception);
                    OnDatabaseError?.Invoke("Failed to load leaderboard");
                    return;
                }
                
                DataSnapshot snapshot = task.Result;
                Debug.Log("[DatabaseManager] Snapshot exists: " + snapshot.Exists);
                Debug.Log("[DatabaseManager] Snapshot has children: " + snapshot.HasChildren);
                Debug.Log("[DatabaseManager] Children count: " + snapshot.ChildrenCount);
                
                List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
                
                // Parse each child snapshot into LeaderboardEntry object
                foreach (DataSnapshot child in snapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    Debug.Log("[DatabaseManager] Raw JSON from Firebase: " + json);
                    
                    LeaderboardEntry entry = JsonUtility.FromJson<LeaderboardEntry>(json);
                    
                    if (entry != null)
                    {
                        Debug.Log($"[DatabaseManager] Parsed entry: userId={entry.userId}, displayName={entry.displayName}, highestAffection={entry.highestAffection}");
                        entries.Add(entry);
                    }
                    else
                    {
                        Debug.LogError("[DatabaseManager] Failed to parse entry from JSON: " + json);
                    }
                }
                
                // Sort entries by score in descending order (highest first)
                entries.Sort((a, b) => b.highestAffection.CompareTo(a.highestAffection));
                
                Debug.Log("[DatabaseManager] Leaderboard loaded: " + entries.Count + " entries");
                
                // Log each entry for debugging
                for (int i = 0; i < entries.Count; i++)
                {
                    Debug.Log($"[DatabaseManager] Entry {i}: {entries[i].displayName} - {entries[i].highestAffection}");
                }
                
                Debug.Log("[DatabaseManager] Invoking OnLeaderboardLoaded event...");
                
                // Notify subscribers with sorted leaderboard data
                OnLeaderboardLoaded?.Invoke(entries);
            });
    }
}