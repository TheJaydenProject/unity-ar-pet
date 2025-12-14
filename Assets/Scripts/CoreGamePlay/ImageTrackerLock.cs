using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class StickyImageTracker : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    [Header("Prefabs spawned for reference images")]
    [SerializeField] private GameObject[] placeablePrefabs;

    [Header("Spawn Root (auto-created if null)")]
    [SerializeField] private Transform spawnedRoot;

    // One instance per reference image name
    private readonly Dictionary<string, GameObject> spawnedPrefabs =
        new Dictionary<string, GameObject>();

    // Has this image ever been tracked in Tracking state at least once?
    private readonly Dictionary<string, bool> hasLocked =
        new Dictionary<string, bool>();

    /// <summary>
    /// Reset all spawned objects (called when returning to menu)
    /// </summary>
    public void ResetSpawnedObjects()
    {
        foreach (var kvp in spawnedPrefabs)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(false);
            }
        }
        
        // Reset locked states so they can be detected again
        var keys = new System.Collections.Generic.List<string>(hasLocked.Keys);
        foreach (var key in keys)
        {
            hasLocked[key] = false;
        }
        
        Debug.Log("[StickyImageTracker] All spawned objects reset");
    }

    void Start()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("StickyImageTracker: trackedImageManager is not assigned.");
            return;
        }

        // Create an inactive root so instantiated prefabs don't run OnEnable yet
        if (spawnedRoot == null)
        {
            var rootGO = new GameObject("SpawnedPrefabsRoot");
            spawnedRoot = rootGO.transform;
        }
        if (spawnedRoot.gameObject.activeSelf)
            spawnedRoot.gameObject.SetActive(false);

        SetupPrefabs();
        trackedImageManager.trackablesChanged.AddListener(OnImagesChanged);
    }

    void OnDestroy()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        }
    }

    void SetupPrefabs()
    {
        foreach (GameObject prefab in placeablePrefabs)
        {
            if (prefab == null) continue;

            GameObject instance = Instantiate(prefab, spawnedRoot);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.name = prefab.name;
            instance.SetActive(false);

            spawnedPrefabs[prefab.name] = instance;
            hasLocked[prefab.name] = false;
        }
    }

    void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var img in args.added)
            UpdateImage(img);

        foreach (var img in args.updated)
            UpdateImage(img);

        // We IGNORE args.removed on purpose so objects stay even when tracking is lost
    }

    void UpdateImage(ARTrackedImage trackedImage)
    {
        if (trackedImage == null)
            return;

        string imageName = trackedImage.referenceImage.name;

        if (!spawnedPrefabs.TryGetValue(imageName, out GameObject obj))
            return;

        TrackingState state = trackedImage.trackingState;

        if (state == TrackingState.Tracking)
        {
            // Ensure the root is active so children can be activated
            if (spawnedRoot != null && !spawnedRoot.gameObject.activeSelf)
                spawnedRoot.gameObject.SetActive(true);
            Vector3 localOffset = new Vector3(0f, 0.55f, 0f);

            obj.transform.position =
                trackedImage.transform.position +
                trackedImage.transform.rotation * localOffset;

            obj.transform.rotation = trackedImage.transform.rotation;

            obj.SetActive(true);

            hasLocked[imageName] = true;   // don't hide it
        }
        else
        {
            // Before first good detection we can hide it
            if (!hasLocked[imageName])
            {
                obj.SetActive(false);
            }
            // After first good detection
            // object stays visible where it was last placed
        }
    }
}
