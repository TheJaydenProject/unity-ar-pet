/// <summary>
/// Author: Jayden Wong
/// Date: 13 December 2025
/// Purpose:
/// Manages the main menu navigation after successful login.
/// Handles transitions between menu, instructions, credits, and leaderboard panels.
/// Coordinates with LoginUI for sign out flow.
/// Manages AR and UI camera switching.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject leaderboardPanel;
    
    [Header("Welcome Text")]
    [SerializeField] private TMP_Text welcomeText;
    
    [Header("Menu Buttons")]
    [SerializeField] private Button findPetButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button signOutButton;
    
    [Header("Back Buttons")]
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private Button leaderboardBackButton;
    
    [Header("Instruction Settings")]
    [SerializeField] private float instructionDisplayTime = 5f;
    
    [Header("Reference to Login UI")]
    [SerializeField] private LoginUI loginUI;
    
    [Header("AR and Camera References")]
    [SerializeField] private GameObject xrOrigin;
    [SerializeField] private Camera xrCamera;
    [SerializeField] private Camera uiCamera;
    
    private bool isTransitioning = false;
    
    private void Start()
    {
        InitializeButtons();
        HideAllPanels();
        
        // Start with UI mode enabled (UI Camera on, XR off)
        SetUIMode(true);
    }
    
    /// <summary>
    /// Switch between UI (menus) and XR (gameplay) cameras.
    /// </summary>
    public void SetUIMode(bool isUIMode)
    {
        // Update MainCamera tag so camera-dependent systems use the active camera.
        if (uiCamera != null)
        {
            uiCamera.gameObject.SetActive(isUIMode);
            
            // Set MainCamera tag when active
            if (isUIMode)
                uiCamera.tag = "MainCamera";
            else
                uiCamera.tag = "Untagged";
        }
        
        if (xrCamera != null)
        {
            xrCamera.gameObject.SetActive(!isUIMode);
            
            // Set MainCamera tag when active
            if (!isUIMode)
                xrCamera.tag = "MainCamera";
            else
                xrCamera.tag = "Untagged";
        }
        
        Debug.Log($"[MenuManager] Camera mode: {(isUIMode ? "UI" : "AR")}");
    }
    
    /// <summary>
    /// Set up all button listeners
    /// </summary>
    private void InitializeButtons()
    {
        if (findPetButton != null)
        {
            findPetButton.onClick.RemoveAllListeners();
            findPetButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnFindPetClicked();
            });
        }
        
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnCreditsClicked();
            });
        }
        
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnLeaderboardClicked();
            });
        }
        
        if (signOutButton != null)
        {
            signOutButton.onClick.RemoveAllListeners();
            signOutButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnSignOutClicked();
            });
        }
        
        if (creditsBackButton != null)
        {
            creditsBackButton.onClick.RemoveAllListeners();
            creditsBackButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnCreditsBackClicked();
            });
        }
        
        if (leaderboardBackButton != null)
        {
            leaderboardBackButton.onClick.RemoveAllListeners();
            leaderboardBackButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                OnLeaderboardBackClicked();
            });
        }
    }
    
    /// <summary>
    /// Called by LoginUI after successful login
    /// </summary>
    public void ShowMenuPanel()
    {
        HideAllPanels();
        
        // Switch to UI Camera (disable AR)
        SetUIMode(true);

        // Reset AR tracked objects when returning to menu
        ResetARTrackedObjects();
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            UpdateWelcomeText();
            Debug.Log("[MenuManager] Menu panel shown");
        }
    }
    
    /// <summary>
    /// Update the welcome text with the user's display name
    /// </summary>
    private void UpdateWelcomeText()
    {
        if (welcomeText == null) return;
        
        if (FirebaseAuthManager.Instance != null && FirebaseAuthManager.Instance.IsUserSignedIn())
        {
            var user = FirebaseAuthManager.Instance.GetCurrentUser();
            string displayName = user?.DisplayName;
            
            if (!string.IsNullOrEmpty(displayName))
            {
                welcomeText.text = $"Welcome, \n{displayName}!";
                Debug.Log($"[MenuManager] Welcome text set to: Welcome, {displayName}!");
            }
            else
            {
                // Fallback to email if display name not available
                string email = user?.Email;
                welcomeText.text = !string.IsNullOrEmpty(email) ? $"Welcome, {email}!" : "Welcome!";
            }
        }
        else
        {
            welcomeText.text = "Welcome!";
        }
    }
    
    /// <summary>
    /// Hide all menu-related panels
    /// </summary>
    private void HideAllPanels()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }
    
    /// <summary>
    /// Handle Find Pet button click
    /// Shows instructions panel for specified duration, then starts game
    /// </summary>
    private void OnFindPetClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Find Pet clicked");
        SetUIMode(false);
        
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        // Show instructions overlay while AR camera is active.
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
            StartCoroutine(HideInstructionsAndStartGame());
        }
        else
        {
            // If no instructions panel, game is already started
            StartGame();
        }
    }
    
    /// <summary>
    /// Coroutine to hide instructions after delay and start the game
    /// </summary>
    private IEnumerator HideInstructionsAndStartGame()
    {
        isTransitioning = true;
        
        // Delay before starting gameplay.
        yield return new WaitForSeconds(instructionDisplayTime);
        
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
        
        StartGame();
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Debug start of gameplay
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[MenuManager] Starting game...");
        Debug.Log("[MenuManager] Game ready - AR camera active");
    }
    
    /// <summary>
    /// Handle Credits button click
    /// </summary>
    private void OnCreditsClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Credits clicked");
        
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }
    
    /// <summary>
    /// Handle Credits back button click
    /// </summary>
    private void OnCreditsBackClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Credits back clicked");
        
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
        
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }
    
    /// <summary>
    /// Handle Leaderboard button click
    /// </summary>
    private void OnLeaderboardClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Leaderboard clicked");
        
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            
            // Fetch leaderboard data
            if (FirebaseDatabaseManager.Instance != null)
            {
                FirebaseDatabaseManager.Instance.FetchLeaderboard(FirebaseConfig.LEADERBOARD_TOP_COUNT);
            }
        }
    }
    
    /// <summary>
    /// Handle Leaderboard back button click
    /// </summary>
    private void OnLeaderboardBackClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Leaderboard back clicked");
        
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
        
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }
    
    /// <summary>
    /// Handle Sign Out button click
    /// </summary>
    private void OnSignOutClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Sign Out clicked");
        
        isTransitioning = true;
        
        // Switch to UI Camera (disable AR)
        SetUIMode(true);

        // Reset AR tracked objects
        ResetARTrackedObjects();
        
        // Sign out from Firebase
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.SignOut();
        }
        
        HideAllPanels();
        if (loginUI != null)
        {
            loginUI.ShowLoginPanelFromMenu();
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Clears AR spawned/tracked objects when returning to menu or signing out.
    /// </summary>
    private void ResetARTrackedObjects()
    {
        StickyImageTracker tracker = FindFirstObjectByType<StickyImageTracker>();
        if (tracker != null)
        {
            tracker.ResetSpawnedObjects();
        }
    }
}