/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Loads Firebase configuration from a .env file in StreamingAssets.
/// Uses UnityWebRequest for Android compatibility (since File.ReadAllText
/// doesn't work with StreamingAssets on Android) and standard file I/O
/// for Editor and standalone builds.
/// Parses key=value pairs and stores connection details for Firebase services.
/// </summary>

using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public static class FirebaseConfig
{
    // Firebase connection details loaded from .env file
    public static string DatabaseURL { get; private set; }
    public static string ApiKey { get; private set; }
    public static string ProjectId { get; private set; }
    
    // Database path constants for organizing data structure
    public const string USERS_PATH = "users";
    public const string LEADERBOARD_PATH = "leaderboard";
    
    // Sub-paths under user nodes
    public const string PROFILE_PATH = "profile";
    public const string SESSIONS_PATH = "sessions";
    public const string TURNS_PATH = "turns";
    
    // Configuration constants
    public const int LEADERBOARD_TOP_COUNT = 10;
    public const int MAX_TURNS_PER_SESSION = 25;
    
    private static bool isLoaded = false;
    
    /// <summary>
    /// Asynchronously loads configuration from .env file in StreamingAssets.
    /// Uses platform-appropriate file reading method:
    /// - Android: UnityWebRequest (required for StreamingAssets access)
    /// - Editor/Standalone: File.ReadAllText (faster, simpler)
    /// Parses key=value format and validates required fields.
    /// Call this before using any Firebase services.
    /// </summary>
    public static IEnumerator LoadConfigAsync(System.Action<bool> callback)
    {
        // Skip loading if already loaded
        if (isLoaded)
        {
            Debug.Log("[FirebaseConfig] Config already loaded");
            callback?.Invoke(true);
            yield break;
        }
        
        // Construct path to .env file in StreamingAssets folder
        string envPath = Path.Combine(Application.streamingAssetsPath, ".env");
        Debug.Log("[FirebaseConfig] Looking for .env at: " + envPath);
        
        string fileContent = null;
        
        // Use different file reading approach based on platform
        if (Application.platform == RuntimePlatform.Android)
        {
            // Android: StreamingAssets are in compressed APK, need UnityWebRequest
            Debug.Log("[FirebaseConfig] Using UnityWebRequest for Android");
            
            UnityWebRequest www = UnityWebRequest.Get(envPath);
            yield return www.SendWebRequest();
            
            // Check if request failed
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FirebaseConfig] Failed to load .env on Android: " + www.error);
                Debug.LogError("[FirebaseConfig] Make sure .env file exists in StreamingAssets folder");
                callback?.Invoke(false);
                yield break;
            }
            
            // Extract text content from web request
            fileContent = www.downloadHandler.text;
        }
        else
        {
            // Editor/Standalone: Can use standard file I/O
            Debug.Log("[FirebaseConfig] Using File.ReadAllText for non-Android platform");
            
            if (!File.Exists(envPath))
            {
                Debug.LogError("[FirebaseConfig] .env file not found at: " + envPath);
                callback?.Invoke(false);
                yield break;
            }
            
            try
            {
                fileContent = File.ReadAllText(envPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FirebaseConfig] Failed to read .env: " + ex.Message);
                callback?.Invoke(false);
                yield break;
            }
        }
        
        // Validate that file content was successfully read
        if (string.IsNullOrEmpty(fileContent))
        {
            Debug.LogError("[FirebaseConfig] .env file is empty");
            callback?.Invoke(false);
            yield break;
        }
        
        try
        {
            var config = new Dictionary<string, string>();
            
            // Split file into individual lines, removing empty entries
            string[] lines = fileContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // Parse each line as key=value pair
            foreach (var line in lines)
            {
                // Skip empty lines and comments (lines starting with #)
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                // Split on first '=' to handle values that contain '='
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;
                
                // Trim whitespace from key and value
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                
                config[key] = value;
            }
            
            // Extract required configuration values
            DatabaseURL = GetConfigValue(config, "FIREBASE_DATABASE_URL");
            ApiKey = GetConfigValue(config, "FIREBASE_API_KEY");
            ProjectId = GetConfigValue(config, "FIREBASE_PROJECT_ID");
            
            // Validate that required field exists
            if (string.IsNullOrEmpty(DatabaseURL))
            {
                Debug.LogError("[FirebaseConfig] FIREBASE_DATABASE_URL not found in .env");
                callback?.Invoke(false);
                yield break;
            }
            
            // Mark as successfully loaded
            isLoaded = true;
            Debug.Log("[FirebaseConfig] Config loaded successfully");
            Debug.Log("[FirebaseConfig] Database URL: " + DatabaseURL);
            
            callback?.Invoke(true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FirebaseConfig] Failed to parse .env: " + ex.Message);
            callback?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Safely retrieves a value from config dictionary, returning null if not found.
    /// </summary>
    private static string GetConfigValue(Dictionary<string, string> config, string key)
    {
        return config.ContainsKey(key) ? config[key] : null;
    }
    
    /// <summary>
    /// Returns whether configuration has been successfully loaded.
    /// </summary>
    public static bool IsConfigLoaded()
    {
        return isLoaded;
    }
}