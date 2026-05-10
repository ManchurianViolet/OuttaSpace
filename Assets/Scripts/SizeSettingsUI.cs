using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 크기 세부 패널. 항해/기능 모드 사이즈 드롭다운 각 1개.
/// 즉시 적용 (PlayerPrefs 저장).
/// </summary>
public class SizeSettingsUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI widgetLabel;
    public TextMeshProUGUI stationLabel;
    public TMP_Dropdown widgetDropdown;
    public TMP_Dropdown stationDropdown;
    public Button closeButton;

    [Header("Reopen After Close")]
    public SettingsPanelUI settingsPanelToReopen;

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        BuildDropdowns();
        RefreshTexts();

        if (widgetDropdown != null)
            widgetDropdown.onValueChanged.AddListener(OnWidgetChanged);
        if (stationDropdown != null)
            stationDropdown.onValueChanged.AddListener(OnStationChanged);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;
    }

    void OnDisable()
    {
        if (widgetDropdown != null)
            widgetDropdown.onValueChanged.RemoveListener(OnWidgetChanged);
        if (stationDropdown != null)
            stationDropdown.onValueChanged.RemoveListener(OnStationChanged);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (titleLabel != null)   titleLabel.text   = Loc.Get("settings_size_title");
        if (widgetLabel != null)  widgetLabel.text  = Loc.Get("settings_widget_label");
        if (stationLabel != null) stationLabel.text = Loc.Get("settings_station_label");
    }

    void BuildDropdowns()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        if (widgetDropdown != null)
        {
            widgetDropdown.ClearOptions();
            var labels = new List<TMP_Dropdown.OptionData>();
            foreach (var p in sm.sizePresets)
                labels.Add(new TMP_Dropdown.OptionData(p.label));
            widgetDropdown.AddOptions(labels);
            widgetDropdown.SetValueWithoutNotify(sm.FindPresetIndexForWidget());
            widgetDropdown.RefreshShownValue();
        }

        if (stationDropdown != null)
        {
            stationDropdown.ClearOptions();
            var labels2 = new List<TMP_Dropdown.OptionData>();
            foreach (var p in sm.sizePresets)
                labels2.Add(new TMP_Dropdown.OptionData(p.label));
            stationDropdown.AddOptions(labels2);
            stationDropdown.SetValueWithoutNotify(sm.FindPresetIndexForStation());
            stationDropdown.RefreshShownValue();
        }
    }

    void OnWidgetChanged(int index)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetWidgetSize(index);
    }

    void OnStationChanged(int index)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetStationSize(index);
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
