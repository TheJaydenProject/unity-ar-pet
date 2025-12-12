using UnityEngine;

public class DisableARInEditor : MonoBehaviour
{
    [SerializeField] GameObject arSession;
    [SerializeField] GameObject xrOrigin;

    void Awake()
    {
#if UNITY_EDITOR
        if (arSession) arSession.SetActive(false);
        if (xrOrigin) xrOrigin.SetActive(false);
#endif
    }
}
