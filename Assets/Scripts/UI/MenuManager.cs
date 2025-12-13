/// <summary>
/// 
/// Author: Jayden Wong
/// Date: 13 December 2025
/// Purpose:
/// Manages the main menu navigation after successful login.
/// Handles transitions between menu, instructions, credits, and leaderboard panels.
/// Coordinates with LoginUI for sign out flow.
/// 
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
    
    private bool isTransitioning = false;
    
    private void Start()
    {
        InitializeButtons();
        
        // Hide all panels initially
        HideAllPanels();
        
        // Menu panel will be shown by LoginUI after successful login
    }
    
    /// <summary>
    /// Set up all button listeners
    /// </summary>
    private void InitializeButtons()
    {
        if (findPetButton != null)
        {
            findPetButton.onClick.RemoveAllListeners();
            findPetButton.onClick.AddListener(OnFindPetClicked);
        }
        
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(OnCreditsClicked);
        }
        
        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }
        
        if (signOutButton != null)
        {
            signOutButton.onClick.RemoveAllListeners();
            signOutButton.onClick.AddListener(OnSignOutClicked);
        }
        
        if (creditsBackButton != null)
        {
            creditsBackButton.onClick.RemoveAllListeners();
            creditsBackButton.onClick.AddListener(OnCreditsBackClicked);
        }
        
        if (leaderboardBackButton != null)
        {
            leaderboardBackButton.onClick.RemoveAllListeners();
            leaderboardBackButton.onClick.AddListener(OnLeaderboardBackClicked);
        }
    }
    
    /// <summary>
    /// Called by LoginUI after successful login
    /// </summary>
    public void ShowMenuPanel()
    {
        HideAllPanels();
        
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
        
        // Hide menu panel
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        // Show instructions panel
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
            StartCoroutine(HideInstructionsAndStartGame());
        }
        else
        {
            // If no instructions panel, go straight to game
            StartGame();
        }
    }
    
    /// <summary>
    /// Coroutine to hide instructions after delay and start the game
    /// </summary>
    private IEnumerator HideInstructionsAndStartGame()
    {
        isTransitioning = true;
        
        // Wait for the specified duration
        yield return new WaitForSeconds(instructionDisplayTime);
        
        // Hide instructions panel
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
        
        // Start the actual game
        StartGame();
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Start the actual gameplay
    /// Override this or call your game start logic here
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[MenuManager] Starting game...");
        
        // TODO: Add your game start logic here
        // For example:
        // - Load game scene
        // - Initialize game manager
        // - Start first turn
        // - Show game UI
        
        // Example: If you have a GameManager, you might do:
        // GameManager.Instance.StartNewGame();
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
    /// Signs user out and returns to login screen
    /// </summary>
    private void OnSignOutClicked()
    {
        if (isTransitioning) return;
        
        Debug.Log("[MenuManager] Sign Out clicked");
        
        isTransitioning = true;
        
        // Sign out from Firebase
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.SignOut();
        }
        
        // Hide all menu panels
        HideAllPanels();
        
        // Show login UI again
        if (loginUI != null)
        {
            loginUI.ShowLoginPanelFromMenu();
        }
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Set button interactivity (useful during transitions)
    /// </summary>
    private void SetButtonsInteractable(bool value)
    {
        if (findPetButton != null) findPetButton.interactable = value;
        if (creditsButton != null) creditsButton.interactable = value;
        if (leaderboardButton != null) leaderboardButton.interactable = value;
        if (signOutButton != null) signOutButton.interactable = value;
        if (creditsBackButton != null) creditsBackButton.interactable = value;
        if (leaderboardBackButton != null) leaderboardBackButton.interactable = value;
    }
}