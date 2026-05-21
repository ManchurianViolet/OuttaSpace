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

        int nextIdx = gm.currentStarIndex + 1;
        bool hasNext = nextIdx < StarDatabase.Stars.Length;

        if (hasNext)
        {
            StarData next = StarDatabase.Stars[nextIdx];
            string nextName = Loc.Get(next.nameKey);

            if (nextDestText != null)
                nextDestText.text = Loc.Get("dock_next", nextName, StarDatabase.FormatKM(next.distanceKM));

            if (etaText != null)
            {
                // 부스터 자동 사이클 평균 속도 반영
                double avgSpeed = ComputeAverageSpeed(gm);
                if (avgSpeed > 0)
                    etaText.text = Loc.Get("dock_eta", GameManager.FormatTime(next.distanceKM / avgSpeed));
            }

            if (departButtonText != null)
                departButtonText.text = Loc.Get("dock_depart", nextName);
        }
        else
        {
            if (nextDestText != null) nextDestText.text = Loc.Get("dock_last_dest");
            if (etaText != null) etaText.text = "";
            if (departButtonText != null) departButtonText.text = Loc.Get("dock_complete");
        }

        if (departButton != null)
            departButton.interactable = hasNext;
    }

    /// <summary>
    /// 부스터 자동 사이클을 반영한 평균 속도 계산.
    /// 부스터는 (충전 시간 / 소진 시간) 사이클로 항상 켜졌다 꺼지므로
    /// 사이클 가중 평균이 실제 진행 속도에 가까움.
    /// </summary>
    double ComputeAverageSpeed(GameManager gm)
    {
        double baseSpeed = gm.GetTotalSpeed();
        if (BoosterSystem.Instance == null) return baseSpeed;

        var bs = BoosterSystem.Instance;
        float chargeTime = bs.autoFillPerSecond > 0 ? 1f / bs.autoFillPerSecond : 50f;
        float drainTime = bs.boostDrainPerSecond > 0 ? 1f / bs.boostDrainPerSecond : 5f;
        float boostMult = gm.GetBoosterSpeedMultiplier();

        // 사이클: (chargeTime 동안 1배) + (drainTime 동안 boostMult배)
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
            if (lastIdx >= 0 && lastIdx < StarDatabase.Stars.Length)
                return StarDatabase.Stars[lastIdx];
        }

        return null;
    }

    void OnDepartClicked()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.Depart();
    }
}