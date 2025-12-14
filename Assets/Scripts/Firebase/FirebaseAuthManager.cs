/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Manages all Firebase Authentication operations including user sign-up,
/// sign-in, password reset, and display name validation.
/// Uses Firebase's async pattern with ContinueWithOnMainThread to ensure
/// all callbacks execute on Unity's main thread for safe UI updates.
/// Implements a custom password reset flow using admin credentials.
/// </summary>

using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }
    
    // Events to notify UI of auth state changes
    public event Action<FirebaseUser> OnAuthSuccess;
    public event Action<string> OnAuthError;
    public event Action OnPasswordResetSuccess;
    
    // Admin credentials for password reset functionality
    private const string ADMIN_EMAIL = "admin@admin.com";
    private const string ADMIN_PASSWORD = "password123";
    
    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Creates a new user account with email and password.
    /// First checks if the display name is already taken to ensure uniqueness.
    /// On success, creates both a Firebase Auth account and a database profile
    /// using the Firebase Auth UID as the primary key.
    /// </summary>
    public void SignUp(string email, string password, string displayName)
    {
        // Validate Firebase initialization before attempting auth operations
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Checking for duplicate display name: " + displayName);
        
        // Step 1: Check if display name already exists in database
        CheckDisplayNameExists(displayName, (exists) =>
        {
            if (exists)
            {
                OnAuthError?.Invoke("Display name already taken. Please choose another.");
                return;
            }
            
            Debug.Log("[Auth] Display name available. Creating account for: " + email);
            
            // Step 2: Create Firebase Auth account
            FirebaseManager.Instance.Auth
                .CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(authTask =>
                {
                    // Check if account creation failed
                    if (authTask.IsFaulted || authTask.IsCanceled)
                    {
                        HandleAuthException(authTask.Exception, "Sign up");
                        return;
                    }
                    
                    FirebaseUser user = authTask.Result.User;
                    Debug.Log("[Auth] Account created. Firebase UID: " + user.UserId);
                    
                    // Step 3: Update Firebase Auth profile with display name
                    UserProfile profile = new UserProfile { DisplayName = displayName };
                    user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.IsFaulted || profileTask.IsCanceled)
                        {
                            Debug.LogWarning("[Auth] Failed to update profile: " + profileTask.Exception);
                        }
                        
                        // Step 4: Create database profile with user data
                        CreateUserProfile(user.UserId, email, displayName, password);
                    });
                });
        });
    }
    
    /// <summary>
    /// Searches the database to check if a display name is already in use.
    /// Performs case-insensitive comparison to prevent near-duplicate names.
    /// Uses callback pattern to return async result.
    /// </summary>
    private void CheckDisplayNameExists(string displayName, Action<bool> callback)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string usersPath = FirebaseConfig.USERS_PATH;
        
        Debug.Log($"[Auth] Starting display name check for: '{displayName}'");
        Debug.Log($"[Auth] Checking path: {usersPath}");
        
        // Fetch all users from database
        dbRef.Child(usersPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to check display name: " + task.Exception);
                // On error, allow creation to proceed (fail open)
                callback?.Invoke(false);
                return;
            }
            
            DataSnapshot snapshot = task.Result;
            
            Debug.Log($"[Auth] Snapshot exists: {snapshot.Exists}, HasChildren: {snapshot.HasChildren}");
            Debug.Log($"[Auth] Children count: {snapshot.ChildrenCount}");
            
            // If no users exist yet, name is available
            if (!snapshot.Exists || !snapshot.HasChildren)
            {
                Debug.Log("[Auth] No users in database yet");
                callback?.Invoke(false);
                return;
            }
            
            // Search through all users to check for matching display name
            bool found = false;
            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {
                Debug.Log($"[Auth] Checking user: {userSnapshot.Key}");
                
                DataSnapshot profileSnapshot = userSnapshot.Child(FirebaseConfig.PROFILE_PATH);
                
                Debug.Log($"[Auth] Profile exists: {profileSnapshot.Exists}");
                
                if (profileSnapshot.Exists)
                {
                    // Parse user data from JSON
                    string json = profileSnapshot.GetRawJsonValue();
                    Debug.Log($"[Auth] Profile JSON: {json}");
                    
                    UserData userData = JsonUtility.FromJson<UserData>(json);
                    
                    Debug.Log($"[Auth] Found displayName: '{userData.displayName}' comparing with '{displayName}'");
                    
                    // Case-insensitive comparison to catch variants like "John" vs "john"
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
    /// Authenticates an existing user with email and password.
    /// On success, triggers OnAuthSuccess event for UI to respond.
    /// </summary>
    public void SignIn(string email, string password)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Signing in: " + email);
        
        // Attempt to sign in with provided credentials
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
                
                // Notify UI that authentication succeeded
                OnAuthSuccess?.Invoke(user);
            });
    }
    
    /// <summary>
    /// Resets a user's password using a multi-step process:
    /// 1. Sign in as admin to gain database access
    /// 2. Find target user and retrieve their old password
    /// 3. Sign in as target user using old password
    /// 4. Update password in Firebase Auth
    /// 5. Update password in database for future resets
    /// 6. Sign out and notify UI of success
    /// </summary>
    public void ResetPassword(string targetEmail, string newPassword)
    {
        if (!FirebaseManager.Instance.IsInitialized)
        {
            OnAuthError?.Invoke("Firebase not initialized");
            return;
        }
        
        Debug.Log("[Auth] Starting password reset process for: " + targetEmail);
        
        // Step 1: Sign in as admin to verify permissions
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
                
                // Step 2: Search database for target user and their stored password
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
                    
                    // Step 3: Sign out admin to switch accounts
                    FirebaseManager.Instance.SignOut();
                    
                    // Step 4: Sign in as target user using their old password
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
                            
                            // Step 5: Update password in Firebase Authentication system
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
                                
                                // Step 6: Update password in database for future reset operations
                                UpdatePasswordInDatabase(userId, newPassword, () =>
                                {
                                    // Step 7: Sign out target user and complete reset
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
    /// Searches database for a user by email and retrieves their stored password.
    /// Returns both userId and password via callback for password reset flow.
    /// </summary>
    private void FindUserByEmailWithOldPassword(string email, Action<string, string> callback)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string usersPath = FirebaseConfig.USERS_PATH;
        
        Debug.Log($"[Auth] Searching for user with email: {email}");
        
        // Fetch all users from database
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
            
            // Search through all users for matching email
            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {
                DataSnapshot profileSnapshot = userSnapshot.Child(FirebaseConfig.PROFILE_PATH);
                
                if (profileSnapshot.Exists)
                {
                    // Parse user data and check for email match
                    string json = profileSnapshot.GetRawJsonValue();
                    UserData userData = JsonUtility.FromJson<UserData>(json);
                    
                    // Case-insensitive email comparison
                    if (userData.email != null && 
                        userData.email.Equals(email, StringComparison.OrdinalIgnoreCase))
                    {
                        foundUserId = userData.userId;
                        foundPassword = userData.password;
                        Debug.Log($"[Auth] Found matching user: {foundUserId}");
                        break;
                    }
                }
            }
            
            callback?.Invoke(foundUserId, foundPassword);
        });
    }
    
    /// <summary>
    /// Updates the stored password in the database profile.
    /// Reads existing profile, modifies password field, and writes back.
    /// </summary>
    private void UpdatePasswordInDatabase(string userId, string newPassword, Action onComplete)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        string profilePath = FirebaseConfig.USERS_PATH + "/" + userId + "/" + FirebaseConfig.PROFILE_PATH;
        
        // Read existing profile from database
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
            
            // Parse existing user data
            string json = snapshot.GetRawJsonValue();
            UserData userData = JsonUtility.FromJson<UserData>(json);
            
            // Update only the password field
            userData.password = newPassword;
            
            // Write updated data back to database
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
    /// Signs out the currently authenticated user.
    /// </summary>
    public void SignOut()
    {
        FirebaseManager.Instance.SignOut();
    }
    
    /// <summary>
    /// Returns the currently authenticated Firebase user, or null if none.
    /// </summary>
    public FirebaseUser GetCurrentUser()
    {
        return FirebaseManager.Instance.CurrentUser;
    }
    
    /// <summary>
    /// Checks if any user is currently signed in.
    /// </summary>
    public bool IsUserSignedIn()
    {
        return FirebaseManager.Instance.IsAuthenticated;
    }
    
    /// <summary>
    /// Creates initial user profile in the database after successful sign-up.
    /// Stores user data including email, display name, and password (for demo
    /// password reset functionality). Uses Firebase Auth UID as the primary key.
    /// </summary>
    private void CreateUserProfile(string firebaseUid, string email, string displayName, string password)
    {
        DatabaseReference dbRef = FirebaseManager.Instance.DatabaseRef;
        
        // Create new user data object with initial values
        UserData userData = new UserData
        {
            userId = firebaseUid,
            email = email,
            displayName = displayName,
            password = password,  // Stored for demo password reset functionality
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            totalGamesPlayed = 0,
            highestAffection = 0
        };
        
        // Convert to JSON for database storage
        string json = JsonUtility.ToJson(userData);
        
        // Construct database path: users/{uid}/profile
        string path = FirebaseConfig.USERS_PATH + "/" + firebaseUid + "/" + FirebaseConfig.PROFILE_PATH;
        
        // Write profile to database
        dbRef.Child(path).SetRawJsonValueAsync(json).ContinueWithOnMainThread(writeTask =>
        {
            if (writeTask.IsFaulted || writeTask.IsCanceled)
            {
                Debug.LogError("[Auth] Failed to create user profile: " + writeTask.Exception);
                OnAuthError?.Invoke("Failed to create user profile in database");
                return;
            }
            
            Debug.Log("[Auth] User profile created in database at: " + path);
            
            // Notify UI of successful authentication
            OnAuthSuccess?.Invoke(FirebaseManager.Instance.CurrentUser);
        });
    }
    
    /// <summary>
    /// Unwraps nested exceptions and handles Firebase-specific auth errors.
    /// Converts error codes to user-friendly messages via GetAuthErrorMessage.
    /// </summary>
    private void HandleAuthException(Exception exception, string operation)
    {
        if (exception == null)
        {
            OnAuthError?.Invoke(operation + " failed");
            return;
        }
        
        string errorMessage = "An error occurred";
        
        // Unwrap nested exceptions to find root cause
        Exception innerException = exception;
        while (innerException.InnerException != null)
        {
            innerException = innerException.InnerException;
        }
        
        // Check if it's a Firebase-specific error
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
    /// Translates Firebase AuthError codes into user-friendly error messages.
    /// Provides clear, actionable feedback for common authentication issues.
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