using UnityEngine;

public class ClickCreditsHandler : MonoBehaviour
{
    void Start()
    {
        if (GlobalInputHook.Instance != null)
            GlobalInputHook.Instance.OnGlobalMouseClick += OnMouseClick;
    }

    void OnMouseClick()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.credits += 1;
            GameManager.Instance.totalCredits += 1;
        }
    }

    void OnDestroy()
    {
        if (GlobalInputHook.Instance != null)
            GlobalInputHook.Instance.OnGlobalMouseClick -= OnMouseClick;
    }
}