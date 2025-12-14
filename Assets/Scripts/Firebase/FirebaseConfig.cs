using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Configuration loader for Firebase services using .env file
/// Reads from StreamingAssets/.env at runtime
/// FIXED: Now works on Android builds using UnityWebRequest
/// </summary>
public static class FirebaseConfig
{
    // Loaded from .env file
    public static string DatabaseURL { get; private set; }
    public static string ApiKey { get; private set; }
    public static string ProjectId { get; private set; }
    
    // Database paths
    public const string USERS_PATH = "users";
    public const string LEADERBOARD_PATH = "leaderboard";
    
    // User sub-paths
    public const string PROFILE_PATH = "profile";
    public const string SESSIONS_PATH = "sessions";
    public const string TURNS_PATH = "turns";
    
    // Leaderboard settings
    public const int LEADERBOARD_TOP_COUNT = 10;
    
    // Session settings
    public const int MAX_TURNS_PER_SESSION = 25;
    
    private static bool isLoaded = false;
    
    /// <summary>
    /// Load configuration from .env file
    /// Call this before using any Firebase services
    /// Now returns IEnumerator for async loading on Android
    /// </summary>
    public static IEnumerator LoadConfigAsync(System.Action<bool> callback)
    {
        if (isLoaded)
        {
            Debug.Log("[FirebaseConfig] Config already loaded");
            callback?.Invoke(true);
            yield break;
        }
        
        string envPath = Path.Combine(Application.streamingAssetsPath, ".env");
        Debug.Log("[FirebaseConfig] Looking for .env at: " + envPath);
        
        string fileContent = null;
        
        // Use UnityWebRequest for Android, File.ReadAllText for Editor/Standalone
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("[FirebaseConfig] Using UnityWebRequest for Android");
            
            UnityWebRequest www = UnityWebRequest.Get(envPath);
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FirebaseConfig] Failed to load .env on Android: " + www.error);
                Debug.LogError("[FirebaseConfig] Make sure .env file exists in StreamingAssets folder");
                callback?.Invoke(false);
                yield break;
            }
            
            fileContent = www.downloadHandler.text;
        }
        else
        {
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
        
        // Parse the file content
        if (string.IsNullOrEmpty(fileContent))
        {
            Debug.LogError("[FirebaseConfig] .env file is empty");
            callback?.Invoke(false);
            yield break;
        }
        
        try
        {
            var config = new Dictionary<string, string>();
            
            string[] lines = fileContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                // Parse key=value
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;
                
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                
                config[key] = value;
            }
            
            // Extract required values
            DatabaseURL = GetConfigValue(config, "FIREBASE_DATABASE_URL");
            ApiKey = GetConfigValue(config, "FIREBASE_API_KEY");
            ProjectId = GetConfigValue(config, "FIREBASE_PROJECT_ID");
            
            // Validate required fields
            if (string.IsNullOrEmpty(DatabaseURL))
            {
                Debug.LogError("[FirebaseConfig] FIREBASE_DATABASE_URL not found in .env");
                callback?.Invoke(false);
                yield break;
            }
            
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
    
    private static string GetConfigValue(Dictionary<string, string> config, string key)
    {
        return config.ContainsKey(key) ? config[key] : null;
    }
    
    /// <summary>
    /// Check if config has been loaded
    /// </summary>
    public static bool IsConfigLoaded()
    {
        return isLoaded;
    }
}