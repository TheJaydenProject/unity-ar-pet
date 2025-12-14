/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Purpose:
/// Manages the leaderboard UI and renders top 10 players from Firebase.
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
    /// Returns the text color for a leaderboard row based on rank and user status.
    /// Top three ranks are highlighted (gold, silver, bronze), the current user is
    /// highlighted separately, and all other entries use the default color.
    /// </summary>
    private Color GetLeaderboardRowColor(int index, bool isCurrentUser)
    {
        if (index == 0) return new Color(242f/255f, 201f/255f, 76f/255f);   // Gold
        if (index == 1) return new Color(209f/255f, 213f/255f, 219f/255f);  // Silver
        if (index == 2) return new Color(198f/255f, 134f/255f, 66f/255f);   // Bronze
        if (isCurrentUser) return new Color(34f/255f, 211f/255f, 238f/255f);// Current user
        return Color.white;
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

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        for (int i = 0; i < 10; i++)
        {
            if (i < entries.Count)
            {
                LeaderboardEntry entry = entries[i];

                // Check if this row is the current user (compute once per row)
                bool isCurrentUser = false;
                if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsUserSignedIn())
                {
                    var currentUser = FirebaseAuthManager.Instance.GetCurrentUser();
                    if (currentUser != null)
                        isCurrentUser = (entry.userId == currentUser.UserId);
                }

                string displayText = isCurrentUser ? "You" : entry.displayName;
                Color rowColor = GetLeaderboardRowColor(i, isCurrentUser);

                if (displayFields[i] != null)
                {
                    displayFields[i].text = displayText;
                    displayFields[i].color = rowColor;
                    displayFields[i].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardUI] display{i + 1} is NULL!");
                }

                if (affectionFields[i] != null)
                {
                    affectionFields[i].text = entry.highestAffection.ToString();
                    affectionFields[i].color = rowColor;
                    affectionFields[i].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardUI] aff{i + 1} is NULL!");
                }
            }
            else
            {
                // No player data exists for this rank, so show placeholder text instead of hiding the row.
                if (displayFields[i] != null)
                {
                    displayFields[i].text = "---";
                    displayFields[i].gameObject.SetActive(true);
                }

                if (affectionFields[i] != null)
                {
                    affectionFields[i].text = "---";
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