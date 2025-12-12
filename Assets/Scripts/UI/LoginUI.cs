using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UI Controller for PawPal AR Login/Registration screen
/// Handles user input validation and UI feedback
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    
    [Header("Login Input Fields")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    
    [Header("Register Input Fields")]
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private TMP_InputField registerDisplayNameInput;
    
    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button forgetPasswordButton;
    
    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    
    private bool isProcessing = false;
    private bool isOnLoginPanel = true;
    
    private void Start()
    {
        // Wait for Firebase to initialize
        if (!FirebaseManager.Instance.IsInitialized)
        {
            ShowStatus("Connecting to Firebase...", false);
            Invoke(nameof(CheckFirebaseReady), 1f);
            return;
        }
        
        InitializeUI();
    }
    
    private void CheckFirebaseReady()
    {
        if (FirebaseManager.Instance.IsInitialized)
        {
            InitializeUI();
        }
        else
        {
            ShowStatus("Failed to connect. Please restart.", true);
            Debug.LogError("[LoginUI] Firebase failed to initialize");
        }
    }
    
    private void InitializeUI()
    {
        // Subscribe to auth events
        FirebaseAuthManager.Instance.OnAuthSuccess += HandleAuthSuccess;
        FirebaseAuthManager.Instance.OnAuthError += HandleAuthError;
        
        // Setup button listeners based on your UI
        // Login button handles login
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        
        // Sign Up button switches to register panel OR registers based on which panel is active
        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        
        // Forget password (optional)
        if (forgetPasswordButton != null)
            forgetPasswordButton.onClick.AddListener(OnForgetPasswordClicked);
        
        // Start with login panel
        ShowLoginPanel();
        HideLoading();
        ClearStatus();
        
        // Check if user is already logged in
        if (FirebaseAuthManager.Instance.IsUserSignedIn())
        {
            var user = FirebaseAuthManager.Instance.GetCurrentUser();
            ShowStatus("Welcome back, " + (user.DisplayName ?? user.Email), false);
            Invoke(nameof(LoadGameScene), 1.5f);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnAuthSuccess -= HandleAuthSuccess;
            FirebaseAuthManager.Instance.OnAuthError -= HandleAuthError;
        }
    }
    
    /// <summary>
    /// Handle Login button click
    /// </summary>
    private void OnLoginButtonClicked()
    {
        if (isProcessing) return;
        
        // If on login panel, perform login
        if (isOnLoginPanel)
        {
            PerformLogin();
        }
        else
        {
            // Switch to login panel
            ShowLoginPanel();
        }
    }
    
    /// <summary>
    /// Handle Sign Up button click
    /// </summary>
    private void OnSignUpButtonClicked()
    {
        if (isProcessing) return;
        
        // If on login panel, switch to register panel
        if (isOnLoginPanel)
        {
            ShowRegisterPanel();
        }
        else
        {
            // On register panel, perform registration
            PerformRegistration();
        }
    }
    
    /// <summary>
    /// Perform user login
    /// </summary>
    private void PerformLogin()
    {
        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;
        
        // Validation
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please fill in all fields.", true);
            return;
        }
        
        if (!IsValidEmail(email))
        {
            ShowStatus("Please enter a valid email address.", true);
            return;
        }
        
        isProcessing = true;
        ShowLoading("Signing in...");
        SetButtonsInteractable(false);
        
        FirebaseAuthManager.Instance.SignIn(email, password);
    }
    
    /// <summary>
    /// Perform user registration
    /// </summary>
    private void PerformRegistration()
    {
        string email = registerEmailInput.text.Trim();
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;
        string displayName = registerDisplayNameInput.text.Trim();
        
        // Validation
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || 
            string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(displayName))
        {
            ShowStatus("Please fill in all fields.", true);
            return;
        }
        
        if (!IsValidEmail(email))
        {
            ShowStatus("Please enter a valid email address.", true);
            return;
        }
        
        if (password.Length < 6)
        {
            ShowStatus("Password must be at least 6 characters.", true);
            return;
        }
        
        if (password != confirmPassword)
        {
            ShowStatus("Passwords do not match.", true);
            return;
        }
        
        if (displayName.Length < 3)
        {
            ShowStatus("Display name must be at least 3 characters.", true);
            return;
        }
        
        isProcessing = true;
        ShowLoading("Creating account...");
        SetButtonsInteractable(false);
        
        FirebaseAuthManager.Instance.SignUp(email, password, displayName);
    }
    
    /// <summary>
    /// Handle forget password button
    /// </summary>
    private void OnForgetPasswordClicked()
    {
        ShowStatus("Password reset feature coming soon!", false);
    }
    
    /// <summary>
    /// Show login panel
    /// </summary>
    private void ShowLoginPanel()
    {
        isOnLoginPanel = true;
        
        if (loginPanel != null)
            loginPanel.SetActive(true);
        
        if (registerPanel != null)
            registerPanel.SetActive(false);
        
        ClearStatus();
        ClearInputFields();
    }
    
    /// <summary>
    /// Show register panel
    /// </summary>
    private void ShowRegisterPanel()
    {
        isOnLoginPanel = false;
        
        if (loginPanel != null)
            loginPanel.SetActive(false);
        
        if (registerPanel != null)
            registerPanel.SetActive(true);
        
        ClearStatus();
        ClearInputFields();
    }
    
    /// <summary>
    /// Called when authentication succeeds
    /// </summary>
    private void HandleAuthSuccess(Firebase.Auth.FirebaseUser user)
    {
        Debug.Log("[LoginUI] Auth success for user: " + (user.DisplayName ?? user.Email));
        
        string welcomeMessage = string.IsNullOrEmpty(user.DisplayName) 
            ? "Welcome, " + user.Email 
            : "Welcome, " + user.DisplayName;
        
        ShowStatus(welcomeMessage, false);
        
        // Small delay for user to see welcome message
        Invoke(nameof(LoadGameScene), 1.5f);
    }
    
    /// <summary>
    /// Called when authentication fails
    /// </summary>
    private void HandleAuthError(string errorMessage)
    {
        Debug.LogError("[LoginUI] Auth error: " + errorMessage);
        
        isProcessing = false;
        HideLoading();
        SetButtonsInteractable(true);
        ShowStatus(errorMessage, true);
    }
    
    /// <summary>
    /// Load the game scene
    /// </summary>
    private void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Debug.Log("[LoginUI] Loading scene: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("[LoginUI] Game scene name not set");
            ShowStatus("Configuration error: Game scene not found.", true);
        }
    }
    
    /// <summary>
    /// Show status message to user
    /// </summary>
    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
            statusText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Clear status message
    /// </summary>
    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show loading indicator
    /// </summary>
    private void ShowLoading(string message = "Loading...")
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
        }
        ShowStatus(message, false);
    }
    
    /// <summary>
    /// Hide loading indicator
    /// </summary>
    private void HideLoading()
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// Enable/disable all buttons
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) 
            loginButton.interactable = interactable;
        
        if (signUpButton != null) 
            signUpButton.interactable = interactable;
        
        if (forgetPasswordButton != null) 
            forgetPasswordButton.interactable = interactable;
    }
    
    /// <summary>
    /// Clear all input fields
    /// </summary>
    private void ClearInputFields()
    {
        if (loginEmailInput != null) 
            loginEmailInput.text = "";
        
        if (loginPasswordInput != null) 
            loginPasswordInput.text = "";
        
        if (registerEmailInput != null) 
            registerEmailInput.text = "";
        
        if (registerPasswordInput != null) 
            registerPasswordInput.text = "";
        
        if (registerConfirmPasswordInput != null) 
            registerConfirmPasswordInput.text = "";
        
        if (registerDisplayNameInput != null) 
            registerDisplayNameInput.text = "";
    }
    
    /// <summary>
    /// Basic email validation
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}