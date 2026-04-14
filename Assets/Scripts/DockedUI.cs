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

        if (wsm != null && wsm.dockedStar != null)
        {
            StarData star = wsm.dockedStar;
            if (starNameText != null)
                starNameText.text = Loc.Get(star.nameKey);
            if (rewardText != null)
                rewardText.text = Loc.Get(star.typeKey) + " · " + Loc.Get(star.descKey);
        }

        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits) + " CR";

        if (currentSpeedText != null)
            currentSpeedText.text = GameManager.FormatSpeed(gm.GetTotalSpeed());

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
                double speed = gm.GetTotalSpeed();
                if (speed > 0)
                    etaText.text = Loc.Get("dock_eta", GameManager.FormatTime(next.distanceKM / speed));
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

    void OnDepartClicked()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.Depart();
    }
}
