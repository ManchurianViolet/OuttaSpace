using UnityEngine;

public class ClickCreditsHandler : MonoBehaviour
{
    // 클릭 크레딧 수입 제한: 초당 최대 10회 (0.1초 쿨다운).
    // 오토클릭/매크로로 비정상적으로 빠르게 입력해도 이 간격보다 잦으면 크레딧이 적립되지 않음.
    private const float MIN_CLICK_INTERVAL = 0.1f;
    private float lastCreditTime = -1f;

    void Start()
    {
        if (GlobalInputHook.Instance != null)
            GlobalInputHook.Instance.OnGlobalMouseClick += OnMouseClick;
    }

    void OnMouseClick()
    {
        if (GameManager.Instance == null) return;

        // 마지막 적립 후 0.1초가 안 지났으면 무시 (초당 10회 상한)
        float now = Time.unscaledTime;
        if (now - lastCreditTime < MIN_CLICK_INTERVAL) return;
        lastCreditTime = now;

        GameManager.Instance.credits += 1;
        GameManager.Instance.totalCredits += 1;
    }

    void OnDestroy()
    {
        if (GlobalInputHook.Instance != null)
            GlobalInputHook.Instance.OnGlobalMouseClick -= OnMouseClick;
    }
}
