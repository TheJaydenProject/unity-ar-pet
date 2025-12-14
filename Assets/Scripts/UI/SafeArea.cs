/// <summary>
/// Author: Jayden Wong
/// Date: 14 December 2025
/// Adjusts a UI RectTransform to respect the device safe area
/// (e.g. notches, rounded corners, system bars).
/// Converts screen-space safe area values into canvas-space offsets
/// and applies them at runtime.
/// </summary>

using UnityEngine;
using UnityEngine.UI;

public class SafeAreaHandler : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect; 
    // Reference canvas RectTransform used to convert screen pixels into UI units.

    private RectTransform rectTransform;

    void Start()
    {
        // RectTransform that will be resized to fit within the safe area.
        rectTransform = GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            Debug.LogError("SafeAreaHandler: canvasRect is not assigned.");
            return;
        }

        // Convert screen pixel ratios to canvas-space ratios.
        float widthRatio  = canvasRect.rect.width  / Screen.width;
        float heightRatio = canvasRect.rect.height / Screen.height;

        // Calculate safe-area offsets in canvas units.
        float offsetTop    = (Screen.safeArea.yMax - Screen.height) * heightRatio;
        float offsetBottom =  Screen.safeArea.yMin * heightRatio;
        float offsetLeft   =  Screen.safeArea.xMin * widthRatio;
        float offsetRight  = (Screen.safeArea.xMax - Screen.width) * widthRatio;

        // Apply safe-area offsets to the UI element.
        rectTransform.offsetMax = new Vector2(offsetRight, offsetTop);
        rectTransform.offsetMin = new Vector2(offsetLeft,  offsetBottom);

        // Expand reference resolution so UI scaling remains consistent.
        var scaler = canvasRect.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            var rr = scaler.referenceResolution;
            scaler.referenceResolution = new Vector2(
                rr.x,
                rr.y + Mathf.Abs(offsetTop) + Mathf.Abs(offsetBottom)
            );
        }
    }
}