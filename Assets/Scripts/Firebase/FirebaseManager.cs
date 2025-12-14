/// <summary>
/// Author: Jayden Wong
/// Date: 12 December 2025
/// Purpose: Starts Firebase for the app and keeps one shared instance across scenes.
/// This script:
/// - Loads FirebaseConfig first (so URLs/keys are available)
/// - Checks Firebase dependencies
/// - Creates shared references for Auth and Realtime Database
/// </summary>

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Firebase Status")]
    [SerializeField] private bool isInitialized = false; 
    // Tracks whether Firebase is ready to use (prevents other scripts from calling too early).

    public FirebaseAuth Auth { get; private set; }
    public DatabaseReference DatabaseRef { get; private set; }
    public FirebaseApp App { get; private set; }
    public bool IsInitialized => isInitialized; 
    public bool IsAuthenticated => Auth != null && Auth.CurrentUser != null;
    public string CurrentUserId => Auth?.CurrentUser?.UserId;
    public FirebaseUser CurrentUser => Auth?.CurrentUser;

    private void Awake()
    {
        // Singleton: destroy duplicates so Firebase is not initialized twice.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load config first because InitializeFirebase uses FirebaseConfig.DatabaseURL.
        StartCoroutine(FirebaseConfig.LoadConfigAsync(success =>
        {
            if (success) InitializeFirebase();
            else Debug.LogError("[FirebaseManager] Cannot initialize - config loading failed");
        }));
    }

    private void InitializeFirebase()
    {
        Debug.Log("[FirebaseManager] Initializing Firebase...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(depTask =>
        {
            DependencyStatus dependencyStatus = depTask.Result;

            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("[FirebaseManager] Could not resolve Firebase dependencies: " + dependencyStatus);
                isInitialized = false;
                return;
            }

            App = FirebaseApp.DefaultInstance;
            Auth = FirebaseAuth.DefaultInstance;

            // DatabaseURL decides which Realtime Database we connect to (custom URL vs default project database).
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

            Auth.StateChanged += OnAuthStateChanged;

            isInitialized = true;
            Debug.Log("[FirebaseManager] Firebase initialized successfully");

            if (Auth.CurrentUser != null)
            {
                Debug.Log("[FirebaseManager] User already signed in: " + Auth.CurrentUser.Email);
            }
        });
    }

    /// <summary>
    /// Runs whenever Firebase detects a sign-in or sign-out.
    /// Useful for debugging login flow and updating UI in other scripts.
    /// </summary>
    private void OnAuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (Auth.CurrentUser != null)
            Debug.Log("[FirebaseManager] Auth state: User signed in - " + Auth.CurrentUser.Email);
        else
            Debug.Log("[FirebaseManager] Auth state: User signed out");
    }

    /// <summary>
    /// Signs out the current Firebase user (if any).
    /// </summary>
    public void SignOut()
    {
        if (Auth == null) return;
        Auth.SignOut();
        Debug.Log("[FirebaseManager] User signed out");
    }

    /// <summary>
    /// Returns a reference to a child path in the Realtime Database.
    /// Example: GetDatabaseReference("users").Child(userId)
    /// </summary>
    public DatabaseReference GetDatabaseReference(string path)
    {
        // Prevent null reference errors if another script calls this before Firebase is ready.
        if (!isInitialized || DatabaseRef == null)
        {
            Debug.LogError("[FirebaseManager] Database not initialized");
            return null;
        }

        // Protect against invalid paths (helps debugging when a typo happens).
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogError("[FirebaseManager] Path is null or empty");
            return null;
        }

        return DatabaseRef.Child(path);
    }

    private void OnDestroy()
    {
        // Cleanup: avoid leaving event listeners behind (prevents duplicate callbacks).
        if (Auth != null)
            Auth.StateChanged -= OnAuthStateChanged;
    }
}