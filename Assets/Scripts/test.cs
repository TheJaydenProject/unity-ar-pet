using System;
using System.IO;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class RtdbSmokeTest : MonoBehaviour
{
    private string adminEmail;
    private string adminPassword;

    void Start()
    {
        LoadEnvFile();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(depTask =>
        {
            if (depTask.Result != DependencyStatus.Available)
            {
                Debug.LogError($"Firebase deps not available: {depTask.Result}");
                return;
            }

            Debug.Log("Firebase ready");
            SignInWithAdminAccount();
        });
    }

    void LoadEnvFile()
    {
        string envPath = Path.Combine(Application.streamingAssetsPath, ".env");
        Debug.Log($"Looking for .env at: {envPath}");

        if (!File.Exists(envPath))
        {
            Debug.LogError($".env file not found at: {envPath}");
            return;
        }

        Debug.Log(".env file found, loading...");

        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            if (key == "FIREBASE_ADMIN_EMAIL")
                adminEmail = value;
            else if (key == "FIREBASE_ADMIN_PASSWORD")
                adminPassword = value;
        }

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            Debug.LogError("Firebase admin credentials not found in .env");
        }
        else
        {
            Debug.Log($"Credentials loaded successfully for: {adminEmail}");
        }
    }

    void SignInWithAdminAccount()
    {
        var auth = FirebaseAuth.DefaultInstance;

        auth.SignInWithEmailAndPasswordAsync(adminEmail, adminPassword)
            .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Admin sign-in failed: " + task.Exception);
                return;
            }

            FirebaseUser user = task.Result.User;
            Debug.Log($"Signed in as {user.UserId} ({user.Email})");
            WriteTestData(user.UserId);
        });
    }

    void WriteTestData(string uid)
    {
        var db = FirebaseDatabase.DefaultInstance;
        var refNode = db.RootReference.Child("smokeTest").Child(uid);

        string rawJson = "{\"hello\":\"world\",\"client\":\"unity\",\"ts\":{\".sv\":\"timestamp\"}}";

        refNode.SetRawJsonValueAsync(rawJson).ContinueWithOnMainThread(writeTask =>
        {
            if (writeTask.IsFaulted || writeTask.IsCanceled)
            {
                Debug.LogError("Write failed: " + writeTask.Exception);
                return;
            }

            Debug.Log("Write OK — reading back...");
            refNode.GetValueAsync().ContinueWithOnMainThread(readTask =>
            {
                if (readTask.IsFaulted || readTask.IsCanceled)
                {
                    Debug.LogError("Read failed: " + readTask.Exception);
                    return;
                }

                Debug.Log("Read OK: " + readTask.Result.GetRawJsonValue());
            });
        });
    }
}