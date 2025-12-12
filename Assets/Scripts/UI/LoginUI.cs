using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Login / Registration UI
/// - Forces sign-out on every app start (Editor + Phone)
/// - No scene switching
/// - Hides login/register panels after successful auth
/// - Uses a StatusPanel + StatusText for messages
/// - Auto-hides the status panel after a short delay (including errors)
/// - No loading indicator
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Status UI")]
    [SerializeField] private GameObject statusPanel;   // panel background object
    [SerializeField] private TMP_Text statusText;      // TMP text inside statusPanel
    [SerializeField] private float statusAutoHideSeconds = 2f;

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

    private bool isProcessing = false;
    private bool isOnLoginPanel = true;

    private float statusHideAt = -1f;
    private bool uiInitialized = false;

    private void Start()
    {
        ClearStatus();
        ShowLoginPanel(); // show immediately (even before firebase is ready)

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
            // keep retrying a few times if you want; for now show message and stay on login UI
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

            // Option B: FORCE SIGN-OUT EVERY TIME
            if (FirebaseAuthManager.Instance.IsUserSignedIn())
            {
                FirebaseAuthManager.Instance.SignOut();
                Debug.Log("[LoginUI] Forced sign-out on app start.");
            }
        }

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }

        if (signUpButton != null)
        {
            signUpButton.onClick.RemoveListener(OnSignUpButtonClicked);
            signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        }

        if (forgetPasswordButton != null)
        {
            forgetPasswordButton.onClick.RemoveListener(OnForgetPasswordClicked);
            forgetPasswordButton.onClick.AddListener(OnForgetPasswordClicked);
        }

        isProcessing = false;
        SetButtonsInteractable(true);
        ClearStatus();
        ShowLoginPanel();
    }

    private void Update()
    {
        if (statusHideAt > 0f && Time.unscaledTime >= statusHideAt)
        {
            statusHideAt = -1f;
            ClearStatus();
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

    private void OnLoginButtonClicked()
    {
        if (isProcessing) return;

        if (isOnLoginPanel)
            PerformLogin();
        else
            ShowLoginPanel();
    }

    private void OnSignUpButtonClicked()
    {
        if (isProcessing) return;

        if (isOnLoginPanel)
            ShowRegisterPanel();
        else
            PerformRegistration();
    }

    private void PerformLogin()
    {
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

        FirebaseAuthManager.Instance.SignIn(email, password);
    }

    private void PerformRegistration()
    {
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

        FirebaseAuthManager.Instance.SignUp(email, password, displayName);
    }

    private void HandleAuthSuccess(Firebase.Auth.FirebaseUser user)
    {
        isProcessing = false;
        SetButtonsInteractable(true);

        ShowStatus("Welcome back!", false);

        // Hide auth UI shortly after success
        Invoke(nameof(HideAuthUI), 1f);
    }

    private void HandleAuthError(string errorMessage)
    {
        isProcessing = false;
        SetButtonsInteractable(true);

        // This will now auto-hide after statusAutoHideSeconds
        ShowStatus("Incorrect email or password. Please try again.", true);
    }

    private void HideAuthUI()
    {
        isProcessing = false;
        SetButtonsInteractable(true);
        ClearStatus();

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
        ClearStatus();
    }

    private void ShowRegisterPanel()
    {
        isOnLoginPanel = false;

        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);

        ClearInputFields();
        ClearStatus();
    }

    private void OnForgetPasswordClicked()
    {
        ShowStatus("Password reset not implemented.", false);
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusPanel != null)
            statusPanel.SetActive(true);

        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
            statusText.gameObject.SetActive(true);
        }

        statusHideAt = Time.unscaledTime + Mathf.Max(0.1f, statusAutoHideSeconds);
    }

    private void ClearStatus()
    {
        statusHideAt = -1f;

        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }

        if (statusPanel != null)
            statusPanel.SetActive(false);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (loginButton != null) loginButton.interactable = value;
        if (signUpButton != null) signUpButton.interactable = value;
        if (forgetPasswordButton != null) forgetPasswordButton.interactable = value;
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