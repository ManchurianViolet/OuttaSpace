using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Top Bar")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI speedText;

    [Header("Bottom Info")]
    public TextMeshProUGUI destinationText;  // "달로 향하는중..." 이 한 줄로 통합
    public Image progressBar;
    public TextMeshProUGUI progressPercentText;

    [Header("Notification")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    private float notifTimer;

    // 애니메이션 점 (. → .. → ...)
    private float dotTimer;
    private int dotCount = 1;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged += UpdateHUD;
            GameManager.Instance.OnNotification += ShowNotification;
        }
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        UpdateHUD();
    }

    void Update()
    {
        UpdateHUD();

        // 점 애니메이션: 0.5초마다 전환
        dotTimer += Time.deltaTime;
        if (dotTimer >= 0.5f)
        {
            dotTimer = 0f;
            dotCount = (dotCount % 3) + 1; // 1 → 2 → 3 → 1
        }

        // 알림 타이머
        if (notificationPanel != null && notificationPanel.activeSelf)
        {
            notifTimer -= Time.deltaTime;
            if (notifTimer <= 0)
                notificationPanel.SetActive(false);
        }
    }

    void UpdateHUD()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 크레딧
        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits);

        // 속도
        if (speedText != null)
            speedText.text = GameManager.FormatSpeed(gm.GetTotalSpeed());

        // 목적지 텍스트: "달로 향하는중..."
        StarData star = StarDatabase.Stars[gm.currentStarIndex];
        if (destinationText != null)
        {
            if (gm.isDocked)
            {
                destinationText.text = $"{star.name}에 도착!";
            }
            else
            {
                string dots = new string('.', dotCount);
                destinationText.text = $"{star.name}(으)로 향하는중{dots}";
            }
        }

        // 진행바
        float progress = star.distanceKM > 0
            ? Mathf.Clamp01((float)(gm.distance / star.distanceKM))
            : 0f;

        if (progressBar != null)
            progressBar.fillAmount = progress;
        if (progressPercentText != null)
        {
            string distStr = StarDatabase.FormatKM(gm.distance);
            string totalStr = StarDatabase.FormatKM(star.distanceKM);
            progressPercentText.text = $"{distStr} / {totalStr}  ({(progress * 100):F1}%)";
        }
    }

    void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;
        notificationText.text = message;
        notificationPanel.SetActive(true);
        notifTimer = 3f;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged -= UpdateHUD;
            GameManager.Instance.OnNotification -= ShowNotification;
        }
    }
}
