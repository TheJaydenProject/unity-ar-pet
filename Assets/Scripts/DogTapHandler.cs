using UnityEngine;

public class DogTapHandler : MonoBehaviour
{
    private Camera arCamera;
    private DogController dogController;

    private void Awake()
    {
        arCamera = Camera.main;
        dogController = GetComponent<DogController>();
    }

    private void Update()
    {
        // ----- MOBILE TOUCH -----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                TryHitDog(ray);
            }
            return;
        }

        // ----- EDITOR MOUSE -----
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
            TryHitDog(ray);
        }
    }

    private void TryHitDog(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.GetComponentInParent<DogController>() == dogController)
            {
                dogController.Shake();
            }
        }
    }
}
