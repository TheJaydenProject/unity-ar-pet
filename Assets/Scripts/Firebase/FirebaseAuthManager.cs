using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;

/// <summary>
/// Handles all Firebase Authentication operations
/// Uses async pattern with ContinueWithOnMainThread
/// </summary>
public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }
    
    // Events to notify UI of auth state changes
    public event Action<FirebaseUser> OnAuthSuccess;
    public event Action<string> OnAuthError;
    
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
    /// Sign up a new user with email and password
    /// Creates user profile in database using Firebase Auth UID
    /// </summary>
    public void SignUp(string email, string password, string displayName)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Creating account for: " + email);
        
        FirebaseManager.Instance.Auth
            .CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsFaulted || authTask.IsCanceled)
                {
                    HandleAuthException(authTask.Exception, "Sign up");
                    return;
                }
                
                FirebaseUser user = authTask.Result.User;
                Debug.Log("[Auth] Account created. Firebase UID: " + user.UserId);
                
                // Update display name in Firebase Auth
                UserProfile profile = new UserProfile { DisplayName = displayName };
                user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(profileTask =>
                {
                    if (profileTask.IsFaulted || profileTask.IsCanceled)
                    {
                        Debug.LogWarning("[Auth] Failed to update profile: " + profileTask.Exception);
                    }
                    
                    // Create user profile in database
                    CreateUserProfile(user.UserId, email, displayName);
                });
            });
    }
    
    /// <summary>
    /// Sign in existing user with email and password
    /// </summary>
    public void SignIn(string email, string password)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Signing in: " + email);
        
        FirebaseManager.Instance.Auth
            .SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsFaulted || authTask.IsCanceled)
                {
                    HandleAuthException(authTask.Exception, "Sign in");
                    return;
                }
                
                FirebaseUser user = authTask.Result.User;
                Debug.Log("[Auth] Signed in successfully. UID: " + user.UserId);
                
                OnAuthSuccess?.Invoke(user);
            });
    }
    
    /// <summary>
    /// Sign out current user
    /// </summary>
    public void SignOut()
    {
        FirebaseManager.Instance.SignOut();
    }
    
    /// <summary>
    /// Get current authenticated user
    /// </summary>
    public FirebaseUser GetCurrentUser()
    {
        return FirebaseManager.Instance.CurrentUser;
    }
    
    /// <summary>
    /// Check if a user is currently signed in
    /// </summary>
    public bool IsUserSignedIn()
    {
        return FirebaseManager.Instance.IsAuthenticated;
    }
    
    /// <summary>
    /// Create initial user profile in database
    /// Uses Firebase Auth UID as the user ID
    /// </summary>
    private void CreateUserProfile(string firebaseUid, string email, string displayName)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        
        // Create user data object
        UserData userData = new UserData
        {
            userId = firebaseUid,
            email = email,
            displayName = displayName,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            totalGamesPlayed = 0,
            highestAffection = 0
        };
        
        // Convert to JSON
        string json = JsonUtility.ToJson(userData);
        
        // Save to database: users/{firebaseUid}/profile
        string path = FirebaseConfig.USERS_PATH + "/" + firebaseUid + "/" + FirebaseConfig.PROFILE_PATH;
        
        dbRef.Child(path).SetRawJsonValueAsync(json).ContinueWithOnMainThread(writeTask =>
        {
            if (writeTask.IsFaulted || writeTask.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to create user profile: " + writeTask.Exception);
                OnAuthError?.Invoke("Failed to create user profile in database");
                return;
            }
            
            Debug.Log("[Auth] User profile created in database at: " + path);
            
            // Profile created successfully, trigger success event
            OnAuthSuccess?.Invoke(FirebaseManager.Instance.CurrentUser);
        });
    }
    
    /// <summary>
    /// Handle authentication exceptions and convert to user-friendly messages
    /// </summary>
    private void HandleAuthException(Exception exception, string operation)
    {
        if (exception == null)
        {
            OnAuthError?.Invoke(operation + " failed");
            return;
        }
        
        string errorMessage = "An error occurred";
        
        // Extract the innermost exception
        Exception innerException = exception;
        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }
        
        // Try to cast to FirebaseException
        if (innerException is Firebase.FirebaseException firebaseEx)

        {
            errorMessage = GetAuthErrorMessage(firebaseEx);
        }
        else
        {
            errorMessage = innerException.Message;
        }
        
        Debug.LogError("[Auth] " + operation + " failed: " + errorMessage);
        OnAuthError?.Invoke(errorMessage);
    }
    
    /// <summary>
    /// Convert Firebase error codes to user-friendly messages
    /// </summary>
    private string GetAuthErrorMessage(Firebase.FirebaseException ex)
    {
        AuthError errorCode = (AuthError)ex.ErrorCode;
        
        switch (errorCode)
        {
            case AuthError.EmailAlreadyInUse:
                return "This email is already registered.";
            
            case AuthError.InvalidEmail:
                return "Invalid email address.";
            
            case AuthError.WeakPassword:
                return "Password is too weak. Use at least 6 characters.";
            
            case AuthError.WrongPassword:
                return "Incorrect password.";
            
            case AuthError.UserNotFound:
                return "No account found with this email.";
            
            case AuthError.NetworkRequestFailed:
                return "Network error. Check your connection.";
            
            case AuthError.TooManyRequests:
                return "Too many attempts. Please try again later.";
            
            case AuthError.UserDisabled:
                return "This account has been disabled.";
            
            case AuthError.InvalidCredential:
                return "Invalid credentials provided.";
            
            default:
                return "Authentication error: " + ex.Message;
        }
    }
}