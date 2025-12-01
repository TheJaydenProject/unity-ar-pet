using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    string path;

    void Awake()
    {
        path = Application.persistentDataPath + "/petdata.json";
    }

    public void Save(PetStats stats)
    {
        string json = JsonUtility.ToJson(stats);
        File.WriteAllText(path, json);
    }

    public PetStats Load()
    {
        if (File.Exists(path))
            return JsonUtility.FromJson<PetStats>(File.ReadAllText(path));

        return new PetStats();
    }
}
