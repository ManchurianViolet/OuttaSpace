using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Top Bar")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI speedText;

    [Header("Bottom Info")]
    public TextMeshProUGUI destinationText;
    public Image progressBar;
    public TextMeshProUGUI progressPercentText;

    [Header("Notification")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    private float notifTimer;

    // 점 애니메이션
    private float dotTimer;
    private int dotCount = 1;

    // 부스트 표시용 색상
    private Color normalSpeedColor = HexColor("#5DCA5D");
    private Color boostSpeedColor = HexColor("#44AAFF");

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

        dotTimer += Time.deltaTime;
        if (dotTimer >= 0.5f)
        {
            dotTimer = 0f;
            dotCount = (dotCount % 3) + 1;
        }

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

        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits);

        // 속도: 부스트 반영된 실제 속도 표시
        if (speedText != null)
        {
            double effectiveSpeed = gm.GetEffectiveSpeed();
            bool boosting = BoosterSystem.Instance != null && BoosterSystem.Instance.isBoosting;

            if (boosting)
                speedText.text = "⚡ " + GameManager.FormatSpeed(effectiveSpeed);
            else
                speedText.text = GameManager.FormatSpeed(effectiveSpeed);

            speedText.color = boosting ? boostSpeedColor : normalSpeedColor;
        }

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

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
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
