// No Firebase yet 

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

[System.Serializable]

public class User
{
    public string email;
    public string password;
}

public class AuthUI : MonoBehaviour
{
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI messageText;

    private string filePath;
    private List<User> users = new List<User>();

    void Start()
    {
        filePath = Application.persistentDataPath + "/users.json";
        LoadUsers();
    }

    void LoadUsers()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            users = JsonUtility.FromJson<UserList>(json).users;
        }
    }

    void SaveUsers()
    {
        UserList wrapper = new UserList();
        wrapper.users = users;
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(filePath, json);
    }

    [System.Serializable]
    public class UserList { public List<User> users; }

    public void SignUp()
    {
        string email = emailField.text;
        string pass = passwordField.text;

        if (email == "" || pass == "")
        {
            messageText.text = "Fields cannot be empty.";
            return;
        }

        foreach (var u in users)
        {
            if (u.email == email)
            {
                messageText.text = "Email already exists.";
                return;
            }
        }

        users.Add(new User { email = email, password = pass });
        SaveUsers();
        messageText.text = "Sign Up successful!";
    }

    public void SignIn()
    {
        string email = emailField.text;
        string pass = passwordField.text;

        foreach (var u in users)
        {
            if (u.email == email && u.password == pass)
            {
                messageText.text = "Welcome back!";
                return;
            }
        }

        messageText.text = "Invalid email or password.";
    }
}
