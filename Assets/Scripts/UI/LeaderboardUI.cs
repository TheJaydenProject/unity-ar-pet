/// <summary>
/// 
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Purpose:
/// Manages the leaderboard UI display with real-time Firebase data.
/// Connects 10 display name fields and 10 affection value fields
/// to show the top players sorted by highest affection.
/// 
/// </summary>

using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Display Name Fields (1-10)")]
    [SerializeField] private TMP_Text display1;
    [SerializeField] private TMP_Text display2;
    [SerializeField] private TMP_Text display3;
    [SerializeField] private TMP_Text display4;
    [SerializeField] private TMP_Text display5;
    [SerializeField] private TMP_Text display6;
    [SerializeField] private TMP_Text display7;
    [SerializeField] private TMP_Text display8;
    [SerializeField] private TMP_Text display9;
    [SerializeField] private TMP_Text display10;
    
    [Header("Affection Value Fields (1-10)")]
    [SerializeField] private TMP_Text aff1;
    [SerializeField] private TMP_Text aff2;
    [SerializeField] private TMP_Text aff3;
    [SerializeField] private TMP_Text aff4;
    [SerializeField] private TMP_Text aff5;
    [SerializeField] private TMP_Text aff6;
    [SerializeField] private TMP_Text aff7;
    [SerializeField] private TMP_Text aff8;
    [SerializeField] private TMP_Text aff9;
    [SerializeField] private TMP_Text aff10;
    
    [Header("Loading/Error Display")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text errorText;
    
    private TMP_Text[] displayFields;
    private TMP_Text[] affectionFields;
    
    private void Awake()
    {
        // Store references in arrays for easy access
        displayFields = new TMP_Text[] 
        { 
            display1, display2, display3, display4, display5,
            display6, display7, display8, display9, display10
        };
        
        affectionFields = new TMP_Text[] 
        { 
            aff1, aff2, aff3, aff4, aff5,
            aff6, aff7, aff8, aff9, aff10
        };
    }
    
    private void OnEnable()
    {
        Debug.Log("[LeaderboardUI] OnEnable called");
        
        // Subscribe to leaderboard events
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.OnLeaderboardLoaded += HandleLeaderboardLoaded;
            FirebaseDatabaseManager.Instance.OnDatabaseError += HandleDatabaseError;
            
            Debug.Log("[LeaderboardUI] Subscribed to Firebase events");
        }
        else
        {
            Debug.LogError("[LeaderboardUI] FirebaseDatabaseManager.Instance is NULL!");
        }
        
        // Fetch leaderboard data when panel is shown
        RefreshLeaderboard();
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        if (FirebaseDatabaseManager.Instance != null)
        {
            FirebaseDatabaseManager.Instance.OnLeaderboardLoaded -= HandleLeaderboardLoaded;
            FirebaseDatabaseManager.Instance.OnDatabaseError -= HandleDatabaseError;
        }
    }
    
    /// <summary>
    /// Refresh leaderboard data from Firebase
    /// </summary>
    public void RefreshLeaderboard()
    {
        Debug.Log("[LeaderboardUI] RefreshLeaderboard called");
        
        if (FirebaseDatabaseManager.Instance == null)
        {
            Debug.LogError("[LeaderboardUI] FirebaseDatabaseManager.Instance is NULL in RefreshLeaderboard!");
            ShowError("Firebase not initialized");
            return;
        }
        
        Debug.Log("[LeaderboardUI] Calling FetchLeaderboard...");
        
        // Show loading state
        ShowLoading(true);
        ClearLeaderboard();
        
        // Fetch top 10 from Firebase
        FirebaseDatabaseManager.Instance.FetchLeaderboard(10);
        
        Debug.Log("[LeaderboardUI] FetchLeaderboard called");
    }
    
    /// <summary>
    /// Handle leaderboard data received from Firebase
    /// </summary>
    private void HandleLeaderboardLoaded(List<LeaderboardEntry> entries)
    {
        ShowLoading(false);
        
        Debug.Log("[LeaderboardUI] HandleLeaderboardLoaded called with " + (entries?.Count ?? 0) + " entries");
        
        if (entries == null || entries.Count == 0)
        {
            ShowError("No leaderboard data available");
            return;
        }
        
        // Hide error if there was one
        if (errorText != null)
            errorText.gameObject.SetActive(false);
        
        // Populate the leaderboard UI
        for (int i = 0; i < 10; i++)
        {
            if (i < entries.Count)
            {
                // We have data for this position
                LeaderboardEntry entry = entries[i];
                
                Debug.Log($"[LeaderboardUI] Position {i+1}: {entry.displayName} - {entry.highestAffection}");
                
                if (displayFields[i] != null)
                {
                    // Check if this is the current user
                    bool isCurrentUser = false;
                    if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsUserSignedIn())
                    {
                        var currentUser = FirebaseAuthManager.Instance.GetCurrentUser();
                        isCurrentUser = (entry.userId == currentUser.UserId);
                    }
                    
                    // Display "You" if it's the current user, otherwise show their name
                    string displayText = isCurrentUser ? "You" : entry.displayName;
                    displayFields[i].text = displayText;
                    
                    // Set color based on position (top 3 colors override "You" color)
                    Color textColor;
                    if (i == 0) // 1st place - Gold
                    {
                        textColor = new Color(242f/255f, 201f/255f, 76f/255f); // #F2C94C
                    }
                    else if (i == 1) // 2nd place - Silver
                    {
                        textColor = new Color(209f/255f, 213f/255f, 219f/255f); // #D1D5DB
                    }
                    else if (i == 2) // 3rd place - Bronze
                    {
                        textColor = new Color(198f/255f, 134f/255f, 66f/255f); // #C68642
                    }
                    else if (isCurrentUser) // Current user (not in top 3) - Cyan
                    {
                        textColor = new Color(34f/255f, 211f/255f, 238f/255f); // #22D3EE (bright cyan)
                    }
                    else // Everyone else
                    {
                        textColor = Color.white;
                    }
                    
                    displayFields[i].color = textColor;
                    displayFields[i].ForceMeshUpdate();
                    displayFields[i].gameObject.SetActive(true);
                    Debug.Log($"[LeaderboardUI] Set display{i+1} to: {displayText}");
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardUI] display{i+1} is NULL!");
                }
                
                if (affectionFields[i] != null)
                {
                    affectionFields[i].text = entry.highestAffection.ToString();
                    
                    // Check if this is the current user
                    bool isCurrentUser = false;
                    if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsUserSignedIn())
                    {
                        var currentUser = FirebaseAuthManager.Instance.GetCurrentUser();
                        isCurrentUser = (entry.userId == currentUser.UserId);
                    }
                    
                    // Set color based on position (same as display name)
                    Color textColor;
                    if (i == 0) // 1st place - Gold
                    {
                        textColor = new Color(242f/255f, 201f/255f, 76f/255f); // #F2C94C
                    }
                    else if (i == 1) // 2nd place - Silver
                    {
                        textColor = new Color(209f/255f, 213f/255f, 219f/255f); // #D1D5DB
                    }
                    else if (i == 2) // 3rd place - Bronze
                    {
                        textColor = new Color(198f/255f, 134f/255f, 66f/255f); // #C68642
                    }
                    else if (isCurrentUser) // Current user (not in top 3) - Cyan
                    {
                        textColor = new Color(34f/255f, 211f/255f, 238f/255f); // #22D3EE (bright cyan)
                    }
                    else // Everyone else
                    {
                        textColor = Color.white;
                    }
                    
                    affectionFields[i].color = textColor;
                    affectionFields[i].ForceMeshUpdate();
                    affectionFields[i].gameObject.SetActive(true);
                    Debug.Log($"[LeaderboardUI] Set aff{i+1} to: {entry.highestAffection}");
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardUI] aff{i+1} is NULL!");
                }
            }
            else
            {
                // No data for this position - show placeholder or hide
                if (displayFields[i] != null)
                {
                    displayFields[i].text = "---";
                    displayFields[i].ForceMeshUpdate();
                    displayFields[i].gameObject.SetActive(true);
                }
                
                if (affectionFields[i] != null)
                {
                    affectionFields[i].text = "---";
                    affectionFields[i].ForceMeshUpdate();
                    affectionFields[i].gameObject.SetActive(true);
                }
            }
        }
        
        Debug.Log("[LeaderboardUI] Finished displaying " + entries.Count + " leaderboard entries");
    }
    
    /// <summary>
    /// Handle database errors
    /// </summary>
    private void HandleDatabaseError(string error)
    {
        ShowLoading(false);
        ShowError("Failed to load leaderboard: " + error);
    }
    
    /// <summary>
    /// Clear all leaderboard entries
    /// </summary>
    private void ClearLeaderboard()
    {
        for (int i = 0; i < 10; i++)
        {
            if (displayFields[i] != null)
            {
                displayFields[i].text = "";
                displayFields[i].gameObject.SetActive(false);
            }
            
            if (affectionFields[i] != null)
            {
                affectionFields[i].text = "";
                affectionFields[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Show/hide loading indicator
    /// </summary>
    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);
    }
    
    /// <summary>
    /// Show error message
    /// </summary>
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        
        Debug.LogError("[LeaderboardUI] " + message);
    }
}