using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Login / Registration UI
/// - Forces sign-out on every app start (Editor + Phone)
/// - No scene switching
/// - Hides login/register panels after successful auth
/// - Uses separate status panels for login and register
/// - Auto-hides the status panel after a short delay (including errors)
/// - No loading indicator
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Login Status UI")]
    [SerializeField] private GameObject loginStatusPanel;   // status panel inside login panel
    [SerializeField] private TMP_Text loginStatusText;      // text inside loginStatusPanel

    [Header("Register Status UI")]
    [SerializeField] private GameObject registerStatusPanel;   // status panel inside register panel
    [SerializeField] private TMP_Text registerStatusText;      // text inside registerStatusPanel

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

    [Header("Login Panel Buttons")]
    [SerializeField] private Button loginButton;              // "Login" button on login panel
    [SerializeField] private Button switchToRegisterButton;   // "Sign Up" button on login panel (switches view)
    [SerializeField] private Button forgetPasswordButton;

    [Header("Register Panel Buttons")]
    [SerializeField] private Button registerButton;           // "Sign Up" button on register panel (creates account)

    private bool isProcessing = false;
    private float statusHideAt = -1f;
    private bool uiInitialized = false;
    private bool isOnLoginPanel = true;  // Track which panel is active
    private enum AuthFlow { None, Login, Register }
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

            // FORCE SIGN-OUT EVERY TIME
            if (FirebaseAuthManager.Instance.IsUserSignedIn())
            {
                FirebaseAuthManager.Instance.SignOut();
                Debug.Log("[LoginUI] Forced sign-out on app start.");
            }
        }

        // Login Panel Buttons
        if (loginButton != null)
        {
            loginButton.onClick.RemoveAllListeners();
            loginButton.onClick.AddListener(PerformLogin);
        }

        if (switchToRegisterButton != null)
        {
            switchToRegisterButton.onClick.RemoveAllListeners();
            switchToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        }

        if (forgetPasswordButton != null)
        {
            forgetPasswordButton.onClick.RemoveAllListeners();
            forgetPasswordButton.onClick.AddListener(OnForgetPasswordClicked);
        }

        // Register Panel Button
        if (registerButton != null)
        {
            registerButton.onClick.RemoveAllListeners();
            registerButton.onClick.AddListener(PerformRegistration);
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

        // Reset flow
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

        Debug.Log("[LoginUI] Login UI hidden. Gameplay continues.");
    }

    private void ShowLoginPanel()
    {
        isOnLoginPanel = true;

        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);

        ClearInputFields();
        ClearAllStatus();
    }

    private void ShowRegisterPanel()
    {
        isOnLoginPanel = false;

        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);

        ClearInputFields();
        ClearAllStatus();
    }

    private void OnForgetPasswordClicked()
    {
        ShowStatus("Password reset not implemented.", false);
    }

    /// <summary>
    /// Show status message on the currently active panel
    /// </summary>
    private void ShowStatus(string message, bool isError)
    {
        GameObject activeStatusPanel = isOnLoginPanel ? loginStatusPanel : registerStatusPanel;
        TMP_Text activeStatusText = isOnLoginPanel ? loginStatusText : registerStatusText;

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

        // Clear login status
        if (loginStatusText != null)
        {
            loginStatusText.text = "";
            loginStatusText.gameObject.SetActive(false);
        }
        if (loginStatusPanel != null)
            loginStatusPanel.SetActive(false);

        // Clear register status
        if (registerStatusText != null)
        {
            registerStatusText.text = "";
            registerStatusText.gameObject.SetActive(false);
        }
        if (registerStatusPanel != null)
            registerStatusPanel.SetActive(false);
    }

    private void SetButtonsInteractable(bool value)
    {
        // Login panel buttons
        if (loginButton != null) loginButton.interactable = value;
        if (switchToRegisterButton != null) switchToRegisterButton.interactable = value;
        if (forgetPasswordButton != null) forgetPasswordButton.interactable = value;

        // Register panel button
        if (registerButton != null) registerButton.interactable = value;
    }

    private void ClearInputFields()
    {
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";

        if (registerEmailInput != null) registerEmailInput.text = "";
        if (registerPasswordInput != null) registerPasswordInput.text = "";
        if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.text = "";
        if (registerDisplayNameInput != null) registerDisplayNameInput.text = "";
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