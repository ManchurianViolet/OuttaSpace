using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

/// <summary>
/// 언어 세부 패널. 드롭다운으로 언어 선택. ExpandForCollection 패턴.
/// </summary>
public class LanguageSettingsUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI titleLabel;
    public TMP_Dropdown languageDropdown;
    public Button closeButton;

    [Header("Reopen After Close")]
    public SettingsPanelUI settingsPanelToReopen;

    private List<string> localeCodes = new List<string>();

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        BuildDropdown();
        RefreshTexts();

        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;
    }

    void OnDisable()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageSelected);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (titleLabel != null)
            titleLabel.text = Loc.Get("settings_language_title");
    }

    void BuildDropdown()
    {
        if (languageDropdown == null) return;

        languageDropdown.ClearOptions();
        localeCodes.Clear();

        var options = new List<TMP_Dropdown.OptionData>();
        int currentIndex = 0;
        string currentCode = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetCurrentLanguageCode() : "ko";

        int i = 0;
        foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
        {
            string code = loc.Identifier.Code;
            string display;
            try
            {
                display = new System.Globalization.CultureInfo(code).NativeName;
                int paren = display.IndexOf('(');
                if (paren > 0) display = display.Substring(0, paren).Trim();
            }
            catch
            {
                display = loc.LocaleName;
            }
            options.Add(new TMP_Dropdown.OptionData(display));
            localeCodes.Add(code);
            if (code == currentCode) currentIndex = i;
            i++;
        }

        languageDropdown.AddOptions(options);
        languageDropdown.SetValueWithoutNotify(currentIndex);
        languageDropdown.RefreshShownValue();
    }

    void OnLanguageSelected(int index)
    {
        if (index < 0 || index >= localeCodes.Count) return;
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.SetLanguage(localeCodes[index]);
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
