/// <summary>
/// 
/// Author: Jayden Wong
/// Date: 12 December 2025
/// Purpose:
/// Handles the login, registration, and password reset UI.
/// Connects user input and buttons to Firebase authentication
/// and displays success/error feedback to the user.
/// 
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject resetPasswordPanel;

    [Header("Login Status UI")]
    [SerializeField] private GameObject loginStatusPanel;
    [SerializeField] private TMP_Text loginStatusText;

    [Header("Register Status UI")]
    [SerializeField] private GameObject registerStatusPanel;
    [SerializeField] private TMP_Text registerStatusText;

    [Header("Reset Password Status UI")]
    [SerializeField] private GameObject resetStatusPanel;
    [SerializeField] private TMP_Text resetStatusText;

    [Header("Status Settings")]
    [SerializeField] private float statusAutoHideSeconds = 2f;

    [Header("Login Input Fields")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;

    [Header("Register Input Fields")]
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private TMP_InputField registerDisplayNameInput;

    [Header("Reset Password Input Fields")]
    [SerializeField] private TMP_InputField resetEmailInput;
    [SerializeField] private TMP_InputField resetNewPasswordInput;
    [SerializeField] private TMP_InputField resetConfirmPasswordInput;

    [Header("Login Panel Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button switchToRegisterButton;
    [SerializeField] private Button forgetPasswordButton;

    [Header("Register Panel Buttons")]
    [SerializeField] private Button registerButton;

    [Header("Reset Password Panel Buttons")]
    [SerializeField] private Button resetPasswordButton;
    [Header("Menu Manager Reference")]
    [SerializeField] private MenuManager menuManager;

    private bool isProcessing = false;
    private float statusHideAt = -1f;
    private bool uiInitialized = false;
    private PanelType currentPanel = PanelType.Login;
    private enum PanelType { Login, Register, ResetPassword } 
    private enum AuthFlow { None, Login, Register, ResetPassword }
    private AuthFlow lastAuthFlow = AuthFlow.None;

    private void Start()
    {
        ClearAllStatus();
        ShowLoginPanel();

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            ShowStatus("Connecting to Firebase...", false);
            Invoke(nameof(CheckFirebaseReady), 0.5f);
            return;
        }

        InitializeUIOnce();
    }

    private void CheckFirebaseReady()
    {
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialized)
        {
            InitializeUIOnce();
        }
        else
        {
            ShowStatus("Still connecting to Firebase...", false);
            Invoke(nameof(CheckFirebaseReady), 0.5f);
        }
    }

    private void InitializeUIOnce()
    {
        if (uiInitialized) return;
        uiInitialized = true;

        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnAuthSuccess += HandleAuthSuccess;
            FirebaseAuthManager.Instance.OnAuthError += HandleAuthError;
            FirebaseAuthManager.Instance.OnPasswordResetSuccess += HandlePasswordResetSuccess;

            // Start from logged-out state so the auth flow can be demoed consistently
            if (FirebaseAuthManager.Instance.IsUserSignedIn())
            {
                FirebaseAuthManager.Instance.SignOut();
                Debug.Log("[LoginUI] Forced sign-out on app start.");
            }
        }

        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                PerformLogin();
            });
        }

        if (switchToRegisterButton != null)
        {
            switchToRegisterButton.onClick.RemoveAllListeners();
            switchToRegisterButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                ShowRegisterPanel();
            });
        }

        if (forgetPasswordButton != null)
        {
            forgetPasswordButton.onClick.RemoveAllListeners();
            forgetPasswordButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                ShowResetPasswordPanel();
            });
        }

        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                PerformRegistration();
            });
        }

        if (resetPasswordButton != null)
        {
            resetPasswordButton.onClick.RemoveAllListeners();
            resetPasswordButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                PerformPasswordReset();
            });
        }

        isProcessing = false;
        SetButtonsInteractable(true);
        ClearAllStatus();
        ShowLoginPanel();
    }

    private void Update()
    {
        if (statusHideAt > 0f && Time.unscaledTime >= statusHideAt)
        {
            statusHideAt = -1f;
            ClearAllStatus();
        }
    }

    private void OnDestroy()
    {
        if (FirebaseAuthManager.Instance != null)
        {
            FirebaseAuthManager.Instance.OnAuthSuccess -= HandleAuthSuccess;
            FirebaseAuthManager.Instance.OnAuthError -= HandleAuthError;
            FirebaseAuthManager.Instance.OnPasswordResetSuccess -= HandlePasswordResetSuccess;
        }
    }

    private void PerformLogin()
    {
        if (isProcessing) return;

        string email = loginEmailInput != null ? loginEmailInput.text.Trim() : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowStatus("Please fill in all fields.", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid email address.", true);
            return;
        }

        if (FirebaseAuthManager.Instance == null)
        {
            ShowStatus("Auth system not ready.", true);
            return;
        }

        isProcessing = true;
        SetButtonsInteractable(false);
        ShowStatus("Signing in...", false);
        lastAuthFlow = AuthFlow.Login;

        FirebaseAuthManager.Instance.SignIn(email, password);
    }

    private void PerformRegistration()
    {
        if (isProcessing) return;

        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";
        string displayName = registerDisplayNameInput != null ? registerDisplayNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword) ||
            string.IsNullOrEmpty(displayName))
        {
            ShowStatus("Please fill in all fields.", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid email address.", true);
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

        if (FirebaseAuthManager.Instance == null)
        {
            ShowStatus("Auth system not ready.", true);
            return;
        }

        isProcessing = true;
        SetButtonsInteractable(false);
        ShowStatus("Creating account...", false);
        lastAuthFlow = AuthFlow.Register;

        FirebaseAuthManager.Instance.SignUp(email, password, displayName);
    }

    private void PerformPasswordReset()
    {
        if (isProcessing) return;

        string email = resetEmailInput != null ? resetEmailInput.text.Trim() : "";
        string newPassword = resetNewPasswordInput != null ? resetNewPasswordInput.text : "";
        string confirmPassword = resetConfirmPasswordInput != null ? resetConfirmPasswordInput.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowStatus("Please fill in all fields.", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowStatus("Invalid email address.", true);
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowStatus("Password must be at least 6 characters.", true);
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowStatus("Passwords do not match.", true);
            return;
        }

        if (FirebaseAuthManager.Instance == null)
        {
            ShowStatus("Auth system not ready.", true);
            return;
        }

        isProcessing = true;
        SetButtonsInteractable(false);
        ShowStatus("Resetting password...", false);
        lastAuthFlow = AuthFlow.ResetPassword;

        FirebaseAuthManager.Instance.ResetPassword(email, newPassword);
    }
    private void HandleAuthSuccess(Firebase.Auth.FirebaseUser user)
    {
        isProcessing = false;
        SetButtonsInteractable(true);

        if (lastAuthFlow == AuthFlow.Register)
        {
            // Show message, then send them back to login panel.
            ShowStatus("Account created. Please log in.", false);
            Invoke(nameof(ShowLoginPanel), 1.5f);
        }
        else
        {
            // Normal login success: enter game (hide auth UI).
            ShowStatus("Welcome back!", false);
            Invoke(nameof(HideAuthUI), 1.5f);
        }

        lastAuthFlow = AuthFlow.None;
    }

    private void HandlePasswordResetSuccess()
    {
        isProcessing = false;
        SetButtonsInteractable(true);

        ShowStatus("Password reset successful! Please log in.", false);
        Invoke(nameof(ShowLoginPanel), 2f);

        lastAuthFlow = AuthFlow.None;
    }

    private void HandleAuthError(string errorMessage)
    {
        isProcessing = false;
        SetButtonsInteractable(true);

        // Normalize for string checks
        string e = (errorMessage ?? "").ToLowerInvariant();

        if (lastAuthFlow == AuthFlow.Register)
        {
            // Build a specific message based on what actually failed
            bool emailTaken =
                e.Contains("email") && (e.Contains("already") || e.Contains("in use") || e.Contains("exists")) ||
                e.Contains("email-already-in-use");

            bool invalidEmail =
                e.Contains("invalid email") ||
                e.Contains("email") && e.Contains("invalid") ||
                e.Contains("invalid-email");

            bool weakPassword =
                e.Contains("password") && (e.Contains("weak") || e.Contains("at least")) ||
                e.Contains("weak-password");

            bool displayNameTaken =
                e.Contains("display name") && (e.Contains("taken") || e.Contains("already") || e.Contains("exists")) ||
                e.Contains("username") && (e.Contains("taken") || e.Contains("already") || e.Contains("exists"));

            // Only say what is actually taken/invalid
            if (emailTaken && displayNameTaken)
            {
                ShowStatus("Email and display name are already taken.", true);
            }
            else if (emailTaken)
            {
                ShowStatus("That email \nis already in use.", true);
            }
            else if (displayNameTaken)
            {
                ShowStatus("That display name is already taken.", true);
            }
            else if (invalidEmail)
            {
                ShowStatus("Please enter a valid email address.", true);
            }
            else if (weakPassword)
            {
                ShowStatus("Password is too weak. Use at least 6 characters.", true);
            }
            else
            {
                // Fallback for any other register error
                ShowStatus("Sign up failed. Please try again.", true);
            }
        }
        else
        {
            // Login flow
            ShowStatus("Incorrect email or password. Please try again.", true);
        }

        // Reset flow after an error so old state doesn’t affect next action
        lastAuthFlow = AuthFlow.None;
    }


    private void HideAuthUI()
    {
        isProcessing = false;
        SetButtonsInteractable(true);
        ClearAllStatus();

        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (resetPasswordPanel != null) resetPasswordPanel.SetActive(false);

        Debug.Log("[LoginUI] Login UI hidden. Showing menu...");
        
        // Show the menu panel
        if (menuManager != null)
        {
            menuManager.ShowMenuPanel();
        }
        else
        {
            Debug.LogError("[LoginUI] MenuManager reference not assigned!");
        }
    }

    /// <summary>
    /// Called by MenuManager when user signs out
    /// </summary>
    public void ShowLoginPanelFromMenu()
    {
        ShowLoginPanel();
    }

    private void ShowLoginPanel()
    {
        currentPanel = PanelType.Login;

        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (resetPasswordPanel != null) resetPasswordPanel.SetActive(false);

        ClearInputFields();
        ClearAllStatus();
    }

    private void ShowRegisterPanel()
    {
        currentPanel = PanelType.Register;

        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (resetPasswordPanel != null) resetPasswordPanel.SetActive(false);

        ClearInputFields();
        ClearAllStatus();
    }

    private void ShowResetPasswordPanel()
    {
        currentPanel = PanelType.ResetPassword;

        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (resetPasswordPanel != null) resetPasswordPanel.SetActive(true);

        ClearInputFields();
        ClearAllStatus();
    }

    /// <summary>
    /// Show status message on the currently active panel
    /// </summary>
    private void ShowStatus(string message, bool isError)
    {
        GameObject activeStatusPanel = null;
        TMP_Text activeStatusText = null;

        switch (currentPanel)
        {
            case PanelType.Login:
                activeStatusPanel = loginStatusPanel;
                activeStatusText = loginStatusText;
                break;
            case PanelType.Register:
                activeStatusPanel = registerStatusPanel;
                activeStatusText = registerStatusText;
                break;
            case PanelType.ResetPassword:
                activeStatusPanel = resetStatusPanel;
                activeStatusText = resetStatusText;
                break;
        }

        if (activeStatusPanel != null)
            activeStatusPanel.SetActive(true);

        if (activeStatusText != null)
        {
            activeStatusText.text = message;
            activeStatusText.color = isError ? Color.red : Color.green;
            activeStatusText.gameObject.SetActive(true);
        }

        statusHideAt = Time.unscaledTime + Mathf.Max(0.1f, statusAutoHideSeconds);
    }

    /// <summary>
    /// Clear status on both panels
    /// </summary>
    private void ClearAllStatus()
    {
        statusHideAt = -1f;

        if (loginStatusText != null)
        {
            loginStatusText.text = "";
            loginStatusText.gameObject.SetActive(false);
        }
        if (loginStatusPanel != null)
            loginStatusPanel.SetActive(false);

        if (registerStatusText != null)
        {
            registerStatusText.text = "";
            registerStatusText.gameObject.SetActive(false);
        }
        if (registerStatusPanel != null)
            registerStatusPanel.SetActive(false);

        if (resetStatusText != null)
        {
            resetStatusText.text = "";
            resetStatusText.gameObject.SetActive(false);
        }
        if (resetStatusPanel != null)
            resetStatusPanel.SetActive(false);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (loginButton != null) loginButton.interactable = value;
        if (switchToRegisterButton != null) switchToRegisterButton.interactable = value;
        if (forgetPasswordButton != null) forgetPasswordButton.interactable = value;

        if (registerButton != null) registerButton.interactable = value;

        if (resetPasswordButton != null) resetPasswordButton.interactable = value;
    }

    private void ClearInputFields()
    {
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";

        if (registerEmailInput != null) registerEmailInput.text = "";
        if (registerPasswordInput != null) registerPasswordInput.text = "";
        if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.text = "";
        if (registerDisplayNameInput != null) registerDisplayNameInput.text = "";

        if (resetEmailInput != null) resetEmailInput.text = "";
        if (resetNewPasswordInput != null) resetNewPasswordInput.text = "";
        if (resetConfirmPasswordInput != null) resetConfirmPasswordInput.text = "";       
    }

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