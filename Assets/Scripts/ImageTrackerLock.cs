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

    // One instance per reference image name
    private readonly Dictionary<string, GameObject> spawnedPrefabs =
        new Dictionary<string, GameObject>();

    // Has this image ever been tracked in Tracking state at least once?
    private readonly Dictionary<string, bool> hasLocked =
        new Dictionary<string, bool>();

    void Start()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("StickyImageTracker: trackedImageManager is not assigned.");
            return;
        }

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

            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
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
            Vector3 localOffset = new Vector3(0f, 0.35f, 0f);

            obj.transform.position =
                trackedImage.transform.position +
                trackedImage.transform.rotation * localOffset;

            obj.transform.rotation = trackedImage.transform.rotation;

            obj.SetActive(true);

            hasLocked[imageName] = true;   // from now on we never hide it
        }
        else // Limited or None
        {
            // Before first good detection we can hide it
            if (!hasLocked[imageName])
            {
                obj.SetActive(false);
            }
            // After first good detection we do nothing:
            // object stays visible where it was last placed
        }
    }
}
