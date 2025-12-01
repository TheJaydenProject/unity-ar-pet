using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PetSpawner : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;

    [Header("Pet Prefab")]
    public GameObject petPrefab;

    private GameObject spawnedPet;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // Tap on plane
        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            // Spawn pet once
            if (spawnedPet == null)
            {
                spawnedPet = Instantiate(petPrefab, pose.position, pose.rotation);

                // ---- Add Required Components Automatically ----
                EnsureComponent<PetStatsHandler>(spawnedPet);

                // Optional training systems (enable these if needed)
                // EnsureComponent<AgilityTraining>(spawnedPet);
                // EnsureComponent<StrengthTraining>(spawnedPet);
                // EnsureComponent<MemoryTraining>(spawnedPet);

                // Ensure the Animator exists
                if (spawnedPet.GetComponent<Animator>() == null)
                    Debug.LogWarning("Pet has no Animator! Tricks won't animate.");

                Debug.Log("Pet spawned with all required systems.");
            }
            else
            {
                // Move pet to new position
                spawnedPet.transform.position = pose.position;
            }
        }
    }

    // Generic helper to add component only if missing
    private T EnsureComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }
}
