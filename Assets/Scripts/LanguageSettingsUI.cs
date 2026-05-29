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
            string display = GetDisplayName(code);
            options.Add(new TMP_Dropdown.OptionData(display));
            localeCodes.Add(code);
            if (code == currentCode) currentIndex = i;
            i++;
        }

        languageDropdown.AddOptions(options);
        languageDropdown.SetValueWithoutNotify(currentIndex);
        languageDropdown.RefreshShownValue();

        // 드롭다운 안의 TMP 텍스트들에 현재 언어 폰트 적용
        // (다국어 표기를 위해 가장 글자 커버리지 좋은 폰트로 통일하는 게 베스트지만
        //  일단 현재 언어 폰트로)
        ApplyDropdownFonts();
    }

    /// <summary>
    /// 각 언어의 native name으로 표시 (한국어 → 한국어, English → English, 日本語 → 日本語).
    /// LocaleName은 OS 환경 따라 달라져서 신뢰 못 함.
    /// </summary>
    string GetDisplayName(string code)
    {
        switch (code)
        {
            case "ko": return "한국어";
            case "en": return "English";
            case "ja": return "日本語";
            case "zh-Hans": return "简体中文";
            default:   return code;
        }
    }

    /// <summary>
    /// 드롭다운 내부 TMP 텍스트들에 현재 언어 폰트 강제 적용.
    /// 드롭다운 옵션은 native name 표기라 모든 언어 글자가 다 나와야 하는데,
    /// 단일 폰트로는 어렵고, 일단 현재 언어 폰트로 통일 (사용자가 영어 보고 있으면
    /// 영어 폰트, 일본어 보고 있으면 일본어 폰트). 일본어 폰트에 한글/영문 다 들어있어야 OK.
    /// </summary>
    void ApplyDropdownFonts()
    {
        if (languageDropdown == null || SettingsManager.Instance == null) return;

        string code = SettingsManager.Instance.GetCurrentLanguageCode();
        var font = SettingsManager.Instance.GetFontForLocale(code);
        if (font == null) return;

        // 드롭다운 captionText + itemText 둘 다 갱신
        if (languageDropdown.captionText != null)
            languageDropdown.captionText.font = font;
        if (languageDropdown.itemText != null)
            languageDropdown.itemText.font = font;
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
