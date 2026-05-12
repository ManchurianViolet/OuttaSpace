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
            Refresh(); // 👈 여기서 다시 갱신
        }
    }

    void Update()
    {
        if (creditsText != null && GameManager.Instance != null)
            creditsText.text = GameManager.FormatCredits(GameManager.Instance.credits) + " CR";
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
            creditsText.text = GameManager.FormatCredits(GameManager.Instance.credits) + " CR";

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