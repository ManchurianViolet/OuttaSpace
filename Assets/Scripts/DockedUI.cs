using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DockedUI : MonoBehaviour
{
    [Header("Star Info")]
    public TextMeshProUGUI starNameText;
    public TextMeshProUGUI creditsText;

    [Header("Next Destination")]
    public TextMeshProUGUI nextDestText;
    public TextMeshProUGUI etaText;

    [Header("Depart Button")]
    public Button departButton;
    public TextMeshProUGUI departButtonText;

    // ETA 깜빡 경고용 (12시간 초과 시)
    private bool etaBlinking;
    private Color etaOriginalColor;
    private bool etaColorCached;

    void OnEnable()
    {
        starNameText.text = Loc.Get("loading_location");

        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.OnStateChanged += OnWindowStateChanged;
        if (departButton != null)
            departButton.onClick.AddListener(OnDepartClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.OnStateChanged -= OnWindowStateChanged;
        if (departButton != null)
            departButton.onClick.RemoveListener(OnDepartClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged -= Refresh;
    }

    void OnWindowStateChanged(WindowStateManager.WindowState state)
    {
        if (state == WindowStateManager.WindowState.Docked)
        {
            Refresh();
        }
    }

    void Update()
    {
        if (creditsText != null && GameManager.Instance != null)
            creditsText.text = GameManager.FormatNumber(GameManager.Instance.credits) + " CR";

        // ETA 12시간 초과 시 빨강 깜빡 + 크기 살짝 펌프
        if (etaBlinking && etaText != null)
        {
            // 원래 색 캐시 (한 번만)
            if (!etaColorCached)
            {
                etaOriginalColor = etaText.color;
                etaColorCached = true;
            }

            // 1초 주기로 빨강 ↔ 원래색
            float t = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            etaText.color = Color.Lerp(etaOriginalColor, new Color(1f, 0.3f, 0.3f), t);

            // 크기 1.0 ~ 1.15 사이 펌핑
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.075f;
            etaText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        StarData star = GetDockedStar(gm);

        if (star != null && !string.IsNullOrEmpty(star.nameKey))
        {
            if (starNameText != null)
                starNameText.text = Loc.Get(star.nameKey);
        }

        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits) + " CR";

        // 무한 모드: 다음 별은 항상 존재 (100 이상은 절차 생성 ABYSS)
        int nextIdx = gm.currentStarIndex + 1;
        {
            StarData next = StarDatabase.GetStar(nextIdx);
            string nextName = Loc.Get(next.nameKey);

            // ETA 계산 → 12시간 초과면 깜빡 + 크기 변화 경고
            double avgSpeed = ComputeAverageSpeed(gm);
            double etaSeconds = avgSpeed > 0 ? next.distanceKM / avgSpeed : 0;
            etaBlinking = (etaSeconds > 43200); // 12시간

            if (!etaBlinking && etaText != null && etaColorCached)
            {
                etaText.color = etaOriginalColor;
                etaText.rectTransform.localScale = Vector3.one;
            }

            if (nextDestText != null)
                nextDestText.text = Loc.Get("dock_next", nextName, StarDatabase.FormatKM(next.distanceKM));

            if (etaText != null && avgSpeed > 0)
                etaText.text = Loc.Get("dock_eta", GameManager.FormatTime(etaSeconds));

            if (departButtonText != null)
            {
                // 다음 별이 무한 모드(ABYSS)면 "심연으로"
                if (nextIdx > StarDatabase.LAST_PLANNED_INDEX)
                    departButtonText.text = Loc.Get("dock_depart_abyss");
                else
                    departButtonText.text = Loc.Get("dock_depart", nextName);
            }

            if (departButton != null)
                departButton.interactable = true;
        }
    }

    double ComputeAverageSpeed(GameManager gm)
    {
        double baseSpeed = gm.GetTotalSpeed();
        if (BoosterSystem.Instance == null) return baseSpeed;

        var bs = BoosterSystem.Instance;
        float chargeTime = bs.autoFillPerSecond > 0 ? 1f / bs.autoFillPerSecond : 50f;
        float drainTime = bs.boostDrainPerSecond > 0 ? 1f / bs.boostDrainPerSecond : 5f;
        float boostMult = gm.GetBoosterSpeedMultiplier();

        double avgMult = (chargeTime + drainTime * boostMult) / (chargeTime + drainTime);
        return baseSpeed * avgMult;
    }

    StarData GetDockedStar(GameManager gm)
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null && wsm.dockedStar != null)
            return wsm.dockedStar;

        if (gm.arrivedStars.Count > 0)
        {
            int lastIdx = gm.arrivedStars[gm.arrivedStars.Count - 1];
            if (lastIdx >= 0)
                return StarDatabase.GetStar(lastIdx);
        }

        return null;
    }

    void OnDepartClicked()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.Depart();
    }
}
