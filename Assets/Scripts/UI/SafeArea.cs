using UnityEngine;
using UnityEngine.UI;

public class SafeAreaOld : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            Debug.LogError("SafeAreaOld: canvasRect is not assigned.");
            return;
        }

        float widthRatio  = canvasRect.rect.width  / Screen.width;
        float heightRatio = canvasRect.rect.height / Screen.height;

        float offsetTop    = (Screen.safeArea.yMax - Screen.height) * heightRatio;
        float offsetBottom =  Screen.safeArea.yMin * heightRatio;
        float offsetLeft   =  Screen.safeArea.xMin * widthRatio;
        float offsetRight  = (Screen.safeArea.xMax - Screen.width) * widthRatio;

        rectTransform.offsetMax = new Vector2(offsetRight, offsetTop);
        rectTransform.offsetMin = new Vector2(offsetLeft,  offsetBottom);

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
