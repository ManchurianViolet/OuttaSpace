using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DockedUI : MonoBehaviour
{
    [Header("Star Info")]
    public TextMeshProUGUI starNameText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI creditsText;

    [Header("Speed Info")]
    public TextMeshProUGUI currentSpeedText;
    public TextMeshProUGUI nextDestText;
    public TextMeshProUGUI etaText;

    [Header("Depart Button")]
    public Button departButton;
    public TextMeshProUGUI departButtonText;

    void OnEnable()
    {
        Refresh();
        if (departButton != null)
            departButton.onClick.AddListener(OnDepartClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged += Refresh;
    }

    void OnDisable()
    {
        if (departButton != null)
            departButton.onClick.RemoveListener(OnDepartClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged -= Refresh;
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        var wsm = WindowStateManager.Instance;
        if (gm == null) return;

        // 현재 도착한 항성 정보
        if (wsm != null && wsm.dockedStar != null)
        {
            StarData star = wsm.dockedStar;
            if (starNameText != null)
                starNameText.text = star.name;
            if (rewardText != null)
                rewardText.text = $"{star.starType} · {star.description}";
        }

        // 크레딧
        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits) + " CR";

        // 현재 속도
        if (currentSpeedText != null)
            currentSpeedText.text = GameManager.FormatSpeed(gm.GetTotalSpeed());

        // ★ 다음 목적지 = currentStarIndex + 1 (현재는 도착한 항성이므로)
        int nextIdx = gm.currentStarIndex + 1;
        bool hasNext = nextIdx < StarDatabase.Stars.Length;

        if (hasNext)
        {
            StarData next = StarDatabase.Stars[nextIdx];

            if (nextDestText != null)
                nextDestText.text = $"다음: {next.name} ({StarDatabase.FormatKM(next.distanceKM)})";

            if (etaText != null)
            {
                double speed = gm.GetTotalSpeed();
                if (speed > 0)
                    etaText.text = $"예상: {FormatTime(next.distanceKM / speed)}";
            }

            if (departButtonText != null)
                departButtonText.text = $"{next.name}(으)로 출발 ▶";
        }
        else
        {
            if (nextDestText != null) nextDestText.text = "마지막 목적지입니다";
            if (etaText != null) etaText.text = "";
            if (departButtonText != null) departButtonText.text = "탐사 완료";
        }

        if (departButton != null)
            departButton.interactable = hasNext;
    }

    void OnDepartClicked()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.Depart();
    }

    static string FormatTime(double sec)
    {
        if (sec < 60) return $"{sec:F0}초";
        if (sec < 3600) return $"{sec / 60:F0}분";
        if (sec < 86400) return $"{sec / 3600:F1}시간";
        return $"{sec / 86400:F1}일";
    }
}
