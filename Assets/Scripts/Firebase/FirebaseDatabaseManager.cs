using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles all Firebase Realtime Database operations
/// Manages game sessions, turn logging, and leaderboard updates
/// </summary>
public class FirebaseDatabaseManager : MonoBehaviour
{
    public static FirebaseDatabaseManager Instance { get; private set; }
    
    // Current session data
    private GameSession currentSession;
    private string currentUserId;
    
    // Events
    public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
    public event Action<string> OnDatabaseError;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Start a new game session for the current user
    /// Call this when the game starts
    /// </summary>
    public void StartNewSession()
    {
        if (!FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[DatabaseManager] Cannot start session - user not authenticated");
            return;
        }
        
        currentUserId = FirebaseManager.Instance.CurrentUserId;
        currentSession = new GameSession();
        
        Debug.Log("[DatabaseManager] New session started: " + currentSession.sessionId);
    }
    
    /// <summary>
    /// Log a single turn to the current session
    /// Call this after each player action (Play, Rest, Feed)
    /// </summary>
    public void LogTurn(int turnNumber, string action, string result, int affectionGained, float energy, float hunger)
    {
        if (currentSession == null)
        {
            Debug.LogWarning("[DatabaseManager] No active session. Starting new session...");
            StartNewSession();
        }
        
        TurnLog turn = new TurnLog(turnNumber, action, result, affectionGained, energy, hunger);
        currentSession.turns.Add(turn);
        
        Debug.Log("[DatabaseManager] Turn " + turnNumber + " logged: " + action + " - " + result);
    }
    
    /// <summary>
    /// End the current session and save to database
    /// Call this when the game ends (after 25 turns)
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
        
        currentSession.endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        currentSession.finalAffection = finalAffection;
        
        Debug.Log("[DatabaseManager] Ending session. Final affection: " + finalAffection);
        
        // Save session to database
        SaveSessionToDatabase();
        
        // Update user stats
        UpdateUserStats(finalAffection);
        
        // Check and update leaderboard if new high score
        UpdateLeaderboardIfNeeded(finalAffection);
    }
    
    /// <summary>
    /// Save the current session to Firebase
    /// Path: users/{userId}/sessions/{sessionId}
    /// </summary>
    private void SaveSessionToDatabase()
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        
        string json = JsonUtility.ToJson(currentSession);
        string path = FirebaseConfig.USERS_PATH + "/" + currentUserId + "/" + 
                     FirebaseConfig.SESSIONS_PATH + "/" + currentSession.sessionId;
        
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
    /// Update user profile stats (totalGamesPlayed, highestAffection)
    /// </summary>
    private void UpdateUserStats(int finalAffection)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string profilePath = FirebaseConfig.USERS_PATH + "/" + currentUserId + "/" + FirebaseConfig.PROFILE_PATH;
        
        // First, read current profile
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
            
            // Parse existing profile
            string json = snapshot.GetRawJsonValue();
            UserData userData = JsonUtility.FromJson<UserData>(json);
            
            // Update stats
            userData.totalGamesPlayed++;
            if (finalAffection > userData.highestAffection)
            {
                userData.highestAffection = finalAffection;
                Debug.Log("[DatabaseManager] New high score: " + finalAffection);
            }
            
            // Save updated profile
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
    /// Update leaderboard if this is a new high score
    /// </summary>
    private void UpdateLeaderboardIfNeeded(int finalAffection)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string leaderboardPath = FirebaseConfig.LEADERBOARD_PATH + "/" + currentUserId;
        
        // Get current user info
        FirebaseUser user = FirebaseManager.Instance.CurrentUser;
        string displayName = user.DisplayName ?? user.Email;
        
        // First check if entry exists
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
                // No entry yet, create new one
                shouldUpdate = true;
            }
            else
            {
                // Entry exists, check if new score is higher
                string json = snapshot.GetRawJsonValue();
                LeaderboardEntry existingEntry = JsonUtility.FromJson<LeaderboardEntry>(json);
                
                if (finalAffection > existingEntry.highestAffection)
                {
                    shouldUpdate = true;
                }
            }
            
            if (shouldUpdate)
            {
                // Create new leaderboard entry
                LeaderboardEntry newEntry = new LeaderboardEntry(currentUserId, displayName, finalAffection);
                string entryJson = JsonUtility.ToJson(newEntry);
                
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
    /// Fetch top leaderboard entries
    /// </summary>
    public void FetchLeaderboard(int limit = 10)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string leaderboardPath = FirebaseConfig.LEADERBOARD_PATH;
        
        Debug.Log("[DatabaseManager] Starting FetchLeaderboard for path: " + leaderboardPath);
        
        dbRef.Child(leaderboardPath).KeepSynced(false);
        
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
                
                // Sort by highest affection (descending)
                entries.Sort((a, b) => b.highestAffection.CompareTo(a.highestAffection));
                
                Debug.Log("[DatabaseManager] Leaderboard loaded: " + entries.Count + " entries");
                
                // Log each entry before sending to UI
                for (int i = 0; i < entries.Count; i++)
                {
                    Debug.Log($"[DatabaseManager] Entry {i}: {entries[i].displayName} - {entries[i].highestAffection}");
                }
                
                Debug.Log("[DatabaseManager] Invoking OnLeaderboardLoaded event...");
                OnLeaderboardLoaded?.Invoke(entries);
            });
    }
}