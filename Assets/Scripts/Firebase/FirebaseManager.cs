using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

/// <summary>
/// Central Firebase manager - initializes all Firebase services
/// Singleton pattern ensures only one instance exists
/// Uses .env configuration for security
/// FIXED: Now properly loads config on Android using coroutine
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    
    [Header("Firebase Status")]
    [SerializeField] private bool isInitialized = false;
    
    // Firebase references
    public FirebaseAuth Auth { get; private set; }
    public DatabaseReference DatabaseRef { get; private set; }
    public FirebaseApp App { get; private set; }
    
    // Quick access properties
    public bool IsInitialized => isInitialized;
    public bool IsAuthenticated => Auth != null && Auth.CurrentUser != null;
    public string CurrentUserId => Auth?.CurrentUser?.UserId;
    public FirebaseUser CurrentUser => Auth?.CurrentUser;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load config first using coroutine, then initialize
        StartCoroutine(FirebaseConfig.LoadConfigAsync((success) =>
        {
            if (success)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("[FirebaseManager] Cannot initialize - config loading failed");
            }
        }));
    }
    
    private void InitializeFirebase()
    {
        Debug.Log("[FirebaseManager] Initializing Firebase...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(depTask =>
        {
            DependencyStatus dependencyStatus = depTask.Result;
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Initialize Firebase services
                App = FirebaseApp.DefaultInstance;
                
                Auth = FirebaseAuth.DefaultInstance;

                // Set database instance from config URL (Realtime Database)
                if (!string.IsNullOrEmpty(FirebaseConfig.DatabaseURL))
                {
                    var db = FirebaseDatabase.GetInstance(App, FirebaseConfig.DatabaseURL);
                    DatabaseRef = db.RootReference;
                    Debug.Log("[FirebaseManager] Database URL set: " + FirebaseConfig.DatabaseURL);
                }
                else
                {
                    DatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
                }
                
                // Set auth persistence (keep user logged in)
                Auth.StateChanged += OnAuthStateChanged;
                
                isInitialized = true;
                Debug.Log("[FirebaseManager] Firebase initialized successfully");
                
                // Check if user is already signed in
                if (Auth.CurrentUser != null)
                {
                    Debug.Log("[FirebaseManager] User already signed in: " + Auth.CurrentUser.Email);
                }
            }
            else
            {
                Debug.LogError("[FirebaseManager] Could not resolve Firebase dependencies: " + dependencyStatus);
                isInitialized = false;
            }
        });
    }
    
    /// <summary>
    /// Called when authentication state changes
    /// </summary>
    private void OnAuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (Auth.CurrentUser != null)
        {
            Debug.Log("[FirebaseManager] Auth state: User signed in - " + Auth.CurrentUser.Email);
        }
        else
        {
            Debug.Log("[FirebaseManager] Auth state: User signed out");
        }
    }
    
    /// <summary>
    /// Sign out the current user
    /// </summary>
    public void SignOut()
    {
        if (Auth != null)
        {
            Auth.SignOut();
            Debug.Log("[FirebaseManager] User signed out");
        }
    }
    
    /// <summary>
    /// Get database reference to a specific path
    /// </summary>
    public DatabaseReference GetDatabaseReference(string path)
    {
        if (!isInitialized || DatabaseRef == null)
        {
            Debug.LogError("[FirebaseManager] Database not initialized");
            return null;
        }
        
        return DatabaseRef.Child(path);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (Auth != null)
        {
            Auth.StateChanged -= OnAuthStateChanged;
        }
    }
}