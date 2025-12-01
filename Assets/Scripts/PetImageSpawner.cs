using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PetImageSpawner : MonoBehaviour
{
    public ARTrackedImageManager imageManager;
    public GameObject petPrefab;

    private Dictionary<string, GameObject> spawnedPets = new Dictionary<string, GameObject>();

    void OnEnable()
    {
        imageManager.trackedImagesChanged += OnImageChanged;
    }

    void OnDisable()
    {
        imageManager.trackedImagesChanged -= OnImageChanged;
    }

    void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
            SpawnPet(trackedImage);

        foreach (var trackedImage in args.updated)
            UpdatePetPosition(trackedImage);

        foreach (var trackedImage in args.removed)
            RemovePet(trackedImage);
    }

    void SpawnPet(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (!spawnedPets.ContainsKey(imageName))
        {
            GameObject pet = Instantiate(
                petPrefab,
                trackedImage.transform.position,
                trackedImage.transform.rotation
            );

            spawnedPets.Add(imageName, pet);
        }
    }

    void UpdatePetPosition(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedPets.ContainsKey(imageName))
        {
            GameObject pet = spawnedPets[imageName];
            pet.transform.position = trackedImage.transform.position;
            pet.transform.rotation = trackedImage.transform.rotation;
        }
    }

    void RemovePet(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedPets.ContainsKey(imageName))
        {
            Destroy(spawnedPets[imageName]);
            spawnedPets.Remove(imageName);
        }
    }
}
