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
    public TMP_Dropdown widgetDropdown;     // 항해모드
    public TMP_Dropdown stationDropdown;    // 기능모드
    public Button closeButton;

    [Header("Reopen After Close")]
    public SettingsPanelUI settingsPanelToReopen;

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        BuildDropdowns();

        if (widgetDropdown != null)
            widgetDropdown.onValueChanged.AddListener(OnWidgetChanged);
        if (stationDropdown != null)
            stationDropdown.onValueChanged.AddListener(OnStationChanged);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    void OnDisable()
    {
        if (widgetDropdown != null)
            widgetDropdown.onValueChanged.RemoveListener(OnWidgetChanged);
        if (stationDropdown != null)
            stationDropdown.onValueChanged.RemoveListener(OnStationChanged);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);
    }

    void BuildDropdowns()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        var labels = new List<TMP_Dropdown.OptionData>();
        foreach (var p in sm.sizePresets)
            labels.Add(new TMP_Dropdown.OptionData(p.label));

        if (widgetDropdown != null)
        {
            widgetDropdown.ClearOptions();
            widgetDropdown.AddOptions(labels);
            widgetDropdown.SetValueWithoutNotify(sm.FindPresetIndexForWidget());
            widgetDropdown.RefreshShownValue();
        }

        if (stationDropdown != null)
        {
            stationDropdown.ClearOptions();
            // 새 List 만들어서 넣기 (같은 참조 쓰면 옵션 공유 이슈 방지)
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
