/// <summary>
/// Author: Jayden Wong
/// Date: 9 December 2025
/// Handles tap/click input for interacting with the dog in AR space.
/// Converts screen touches (mobile) or mouse clicks (editor) into raycasts
/// that detect if the player tapped on the dog model. When the dog is hit,
/// triggers the Shake animation as a friendly greeting response.
/// Uses Physics.Raycast to perform 3D collision detection from camera.
/// Works on both mobile devices (touch input) and Unity Editor (mouse input).
/// </summary>

using UnityEngine;

public class DogTapHandler : MonoBehaviour
{
    // Main camera used for converting screen positions to world-space rays
    private Camera arCamera;
    
    // Reference to the dog controller to trigger shake animation
    private DogController dogController;

    private void Awake()
    {
        // Cache camera reference (typically the AR camera)
        arCamera = Camera.main;
        
        // Get DogController component attached to this GameObject
        dogController = GetComponent<DogController>();
    }

    private void Update()
    {
        // ----- MOBILE TOUCH -----
        // Check for touch input (used on mobile devices)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Only process when finger first touches screen (not while dragging)
            if (touch.phase == TouchPhase.Began)
            {
                // Convert touch position to a ray shooting into the scene
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                TryHitDog(ray);
            }
            return; // Skip mouse check if touch is detected
        }

        // ----- EDITOR MOUSE -----
        // Check for mouse click (used in Unity Editor for testing)
        if (Input.GetMouseButtonDown(0))
        {
            // Convert mouse position to a ray shooting into the scene
            Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
            TryHitDog(ray);
        }
    }

    /// <summary>
    /// Casts a ray into the scene and checks if it hits this dog.
    /// If the ray hits any collider that belongs to this dog's hierarchy,
    /// triggers the Shake animation as a response to the tap.
    /// Uses GetComponentInParent to handle hits on child objects (like limbs).
    /// </summary>
    private void TryHitDog(Ray ray)
    {
        // Perform raycast to see if ray hits any collider in the scene
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the hit object is part of this dog's GameObject hierarchy
            // GetComponentInParent handles cases where we hit a child collider
            if (hit.transform.GetComponentInParent<DogController>() == dogController)
            {
                // Player successfully tapped the dog - trigger shake animation
                dogController.Shake();
            }
        }
    }
}