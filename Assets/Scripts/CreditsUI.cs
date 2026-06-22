using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 크레딧 세부 패널. 일단 placeholder.
/// </summary>
public class CreditsUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI titleLabel;
    public Button closeButton;

    [Header("Reopen After Close")]
    public SettingsPanelUI settingsPanelToReopen;

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnCreditsViewed();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;

        RefreshTexts();
    }

    void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (titleLabel != null)
            titleLabel.text = Loc.Get("settings_credits_title");
    }

    void OnClose()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.CollapseFromCollection();

        gameObject.SetActive(false);

        if (settingsPanelToReopen != null)
            settingsPanelToReopen.Reopen();
    }
}
