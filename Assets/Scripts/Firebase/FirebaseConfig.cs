using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Configuration loader for Firebase services using .env file
/// Reads from StreamingAssets/.env at runtime
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
    /// </summary>
    public static bool LoadConfig()
    {
        if (isLoaded)
        {
            Debug.Log("[FirebaseConfig] Config already loaded");
            return true;
        }
        
        string envPath = Path.Combine(Application.streamingAssetsPath, ".env");
        Debug.Log("[FirebaseConfig] Looking for .env at: " + envPath);
        
        if (!File.Exists(envPath))
        {
            Debug.LogError("[FirebaseConfig] .env file not found at: " + envPath);
            Debug.LogError("[FirebaseConfig] Please create StreamingAssets/.env file with Firebase credentials");
            return false;
        }
        
        try
        {
            var config = new Dictionary<string, string>();
            
            foreach (var line in File.ReadAllLines(envPath))
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
                return false;
            }
            
            isLoaded = true;
            Debug.Log("[FirebaseConfig] Config loaded successfully");
            Debug.Log("[FirebaseConfig] Database URL: " + DatabaseURL);
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FirebaseConfig] Failed to load .env: " + ex.Message);
            return false;
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