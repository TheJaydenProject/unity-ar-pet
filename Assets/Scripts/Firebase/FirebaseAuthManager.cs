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
    public event Action OnPasswordResetSuccess;  // NEW
    
    // Admin credentials for password reset
    private const string ADMIN_EMAIL = "admin@admin.com";
    private const string ADMIN_PASSWORD = "password123";
    
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
    /// Now checks for duplicate display names before creating account
    /// </summary>
    public void SignUp(string email, string password, string displayName)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Checking for duplicate display name: " + displayName);
        
        CheckDisplayNameExists(displayName, (exists) =>
        {
            if (exists)
            {
                OnAuthError?.Invoke("Display name already taken. Please choose another.");
                return;
            }
            
            Debug.Log("[Auth] Display name available. Creating account for: " + email);
            
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
                    
                    UserProfile profile = new UserProfile { DisplayName = displayName };
                    user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.IsFaulted || profileTask.IsCanceled)
                        {
                            Debug.LogWarning("[Auth] Failed to update profile: " + profileTask.Exception);
                        }
                        
                        CreateUserProfile(user.UserId, email, displayName, password);
                    });
                });
        });
    }
    
    /// <summary>
    /// Check if a display name already exists in the database
    /// </summary>
    private void CheckDisplayNameExists(string displayName, Action<bool> callback)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string usersPath = FirebaseConfig.USERS_PATH;
        
        Debug.Log($"[Auth] Starting display name check for: '{displayName}'");
        Debug.Log($"[Auth] Checking path: {usersPath}");
        
        dbRef.Child(usersPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to check display name: " + task.Exception);
                callback?.Invoke(false);
                return;
            }
            
            DataSnapshot snapshot = task.Result;
            
            Debug.Log($"[Auth] Snapshot exists: {snapshot.Exists}, HasChildren: {snapshot.HasChildren}");
            Debug.Log($"[Auth] Children count: {snapshot.ChildrenCount}");
            
            if (!snapshot.Exists || !snapshot.HasChildren)
            {
                Debug.Log("[Auth] No users in database yet");
                callback?.Invoke(false);
                return;
            }
            
            bool found = false;
            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {
                Debug.Log($"[Auth] Checking user: {userSnapshot.Key}");
                
                DataSnapshot profileSnapshot = userSnapshot.Child(FirebaseConfig.PROFILE_PATH);
                
                Debug.Log($"[Auth] Profile exists: {profileSnapshot.Exists}");
                
                if (profileSnapshot.Exists)
                {
                    string json = profileSnapshot.GetRawJsonValue();
                    Debug.Log($"[Auth] Profile JSON: {json}");
                    
                    UserData userData = JsonUtility.FromJson<UserData>(json);
                    
                    Debug.Log($"[Auth] Found displayName: '{userData.displayName}' comparing with '{displayName}'");
                    
                    if (userData.displayName != null && 
                        userData.displayName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("[Auth] MATCH FOUND! Display name already exists!");
                        found = true;
                        break;
                    }
                }
            }
            
            Debug.Log($"[Auth] Display name check complete. Found: {found}");
            callback?.Invoke(found);
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
    /// Reset password for a user
    /// Uses admin account to verify, stores new password hash in database
    /// </summary>
    public void ResetPassword(string targetEmail, string newPassword)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Starting password reset process for: " + targetEmail);
        
        // Step 1: Sign in as admin to verify we have permission
        Debug.Log("[Auth] Signing in as admin...");
        FirebaseManager.Instance.Auth
            .SignInWithEmailAndPasswordAsync(ADMIN_EMAIL, ADMIN_PASSWORD)
            .ContinueWithOnMainThread(adminLoginTask =>
            {
                if (adminLoginTask.IsFaulted || adminLoginTask.IsCanceled)
                {
                    Debug.LogError("[Auth] Admin login failed: " + adminLoginTask.Exception);
                    OnAuthError?.Invoke("Password reset system error. Please try again.");
                    return;
                }
                
                Debug.Log("[Auth] Admin logged in successfully");
                
                // Step 2: Find the target user in database
                FindUserByEmailWithOldPassword(targetEmail, (userId, oldPassword) =>
                {
                    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(oldPassword))
                    {
                        Debug.LogError("[Auth] User not found or no password stored: " + targetEmail);
                        FirebaseManager.Instance.SignOut();
                        OnAuthError?.Invoke("No account found with this email.");
                        return;
                    }
                    
                    Debug.Log("[Auth] User found with ID: " + userId);
                    Debug.Log("[Auth] Signing out admin and signing in as target user...");
                    
                    // Step 3: Sign out admin
                    FirebaseManager.Instance.SignOut();
                    
                    // Step 4: Sign in as target user with their old password
                    FirebaseManager.Instance.Auth
                        .SignInWithEmailAndPasswordAsync(targetEmail, oldPassword)
                        .ContinueWithOnMainThread(userLoginTask =>
                        {
                            if (userLoginTask.IsFaulted || userLoginTask.IsCanceled)
                            {
                                Debug.LogError("[Auth] Failed to sign in as target user: " + userLoginTask.Exception);
                                OnAuthError?.Invoke("Could not access user account. Password may have been changed.");
                                return;
                            }
                            
                            FirebaseUser targetUser = userLoginTask.Result.User;
                            Debug.Log("[Auth] Signed in as target user: " + targetUser.Email);
                            
                            // Step 5: Update their password in Firebase Auth
                            targetUser.UpdatePasswordAsync(newPassword).ContinueWithOnMainThread(updateTask =>
                            {
                                if (updateTask.IsFaulted || updateTask.IsCanceled)
                                {
                                    Debug.LogError("[Auth] Failed to update password: " + updateTask.Exception);
                                    FirebaseManager.Instance.SignOut();
                                    OnAuthError?.Invoke("Failed to update password. Please try again.");
                                    return;
                                }
                                
                                Debug.Log("[Auth] Password updated successfully in Firebase Auth");
                                
                                // Step 6: Update password in database for future resets
                                UpdatePasswordInDatabase(userId, newPassword, () =>
                                {
                                    // Step 7: Sign out the target user
                                    Debug.Log("[Auth] Signing out target user...");
                                    FirebaseManager.Instance.SignOut();
                                    Debug.Log("[Auth] Password reset complete!");
                                    
                                    OnPasswordResetSuccess?.Invoke();
                                });
                            });
                        });
                });
            });
    }
    
    /// <summary>
    /// Find user by email and get their current password from database
    /// </summary>
    private void FindUserByEmailWithOldPassword(string email, Action<string, string> callback)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string usersPath = FirebaseConfig.USERS_PATH;
        
        Debug.Log($"[Auth] Searching for user with email: {email}");
        
        dbRef.Child(usersPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to search for user: " + task.Exception);
                callback?.Invoke(null, null);
                return;
            }
            
            DataSnapshot snapshot = task.Result;
            
            if (!snapshot.Exists || !snapshot.HasChildren)
            {
                Debug.Log("[Auth] No users found in database");
                callback?.Invoke(null, null);
                return;
            }
            
            string foundUserId = null;
            string foundPassword = null;
            
            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {
                DataSnapshot profileSnapshot = userSnapshot.Child(FirebaseConfig.PROFILE_PATH);
                
                if (profileSnapshot.Exists)
                {
                    string json = profileSnapshot.GetRawJsonValue();
                    UserData userData = JsonUtility.FromJson<UserData>(json);
                    
                    if (userData.email != null && 
                        userData.email.Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        foundUserId = userData.userId;
                        foundPassword = userData.password; // Get stored password
                        Debug.Log($"[Auth] Found matching user: {foundUserId}");
                        break;
                    }
                }
            }
            
            callback?.Invoke(foundUserId, foundPassword);
        });
    }
    
    /// <summary>
    /// Update password in database
    /// </summary>
    private void UpdatePasswordInDatabase(string userId, string newPassword, Action onComplete)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string profilePath = FirebaseConfig.USERS_PATH + "/" + userId + "/" + FirebaseConfig.PROFILE_PATH;
        
        // Read existing profile
        dbRef.Child(profilePath).GetValueAsync().ContinueWithOnMainThread(readTask =>
        {
            if (readTask.IsFaulted || readTask.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to read user profile: " + readTask.Exception);
                onComplete?.Invoke();
                return;
            }
            
            DataSnapshot snapshot = readTask.Result;
            if (!snapshot.Exists)
            {
                Debug.LogError("[Auth] User profile not found");
                onComplete?.Invoke();
                return;
            }
            
            // Get existing user data
            string json = snapshot.GetRawJsonValue();
            UserData userData = JsonUtility.FromJson<UserData>(json);
            
            // Update password field
            userData.password = newPassword;
            
            // Save back to database
            string updatedJson = JsonUtility.ToJson(userData);
            dbRef.Child(profilePath).SetRawJsonValueAsync(updatedJson).ContinueWithOnMainThread(writeTask =>
            {
                if (writeTask.IsFaulted || writeTask.IsCanceled)
                {
                    Debug.LogError("[Auth] Failed to update password in database: " + writeTask.Exception);
                }
                else
                {
                    Debug.Log("[Auth] Password updated in database");
                }
                
                onComplete?.Invoke();
            });
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
    /// NOW STORES PASSWORD for demo password reset functionality
    /// </summary>
    private void CreateUserProfile(string firebaseUid, string email, string displayName, string password)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        
        UserData userData = new UserData
        {
            userId = firebaseUid,
            email = email,
            displayName = displayName,
            password = password,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            totalGamesPlayed = 0,
            highestAffection = 0
        };
        
        string json = JsonUtility.ToJson(userData);
        
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
        
        Exception innerException = exception;
        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }
        
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