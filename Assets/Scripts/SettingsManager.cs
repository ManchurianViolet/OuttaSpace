using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 설정 통합 관리: 언어, 창 사이즈, 언어별 폰트.
/// 빈 GameObject에 붙이고 Inspector에서 폰트 매핑 + 사이즈 프리셋 설정.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Language Fonts")]
    [Tooltip("언어 코드별 TMP_FontAsset 매핑. 언어 변경 시 모든 텍스트의 폰트를 강제 교체.")]
    public List<LanguageFontPair> languageFonts = new List<LanguageFontPair>();

    [Header("Size Presets (Width × Height)")]
    [Tooltip("드롭다운에 표시될 사이즈 프리셋. 보통 384×256의 정수 배수.")]
    public List<SizePreset> sizePresets = new List<SizePreset>();

    public event Action OnLanguageChanged;

    [System.Serializable]
    public class LanguageFontPair
    {
        public string localeCode;        // "ko", "en"
        public TMP_FontAsset fontAsset;
    }

    [System.Serializable]
    public class SizePreset
    {
        public int width;
        public int height;
        public string label;             // 드롭다운 표시명 (예: "1× (384×256)")
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 프리셋 비어있으면 기본값 채워넣기
        if (sizePresets == null || sizePresets.Count == 0)
            BuildDefaultPresets();
    }

    void Start()
    {
        // 시작 시 현재 언어에 맞는 폰트 강제 적용
        StartCoroutine(ApplyFontsAfterInit());
    }

    IEnumerator ApplyFontsAfterInit()
    {
        // Localization 초기화 대기
        yield return LocalizationSettings.InitializationOperation;
        ApplyFontForCurrentLocale();
    }

    void BuildDefaultPresets()
    {
        sizePresets = new List<SizePreset>
{
    new SizePreset { width = 384,  height = 256,  label = "1x (384x256)" },
    new SizePreset { width = 768,  height = 512,  label = "2x (768x512)" },
    new SizePreset { width = 1152, height = 768,  label = "3x (1152x768)" },
    new SizePreset { width = 1536, height = 1024, label = "4x (1536x1024)" },
    new SizePreset { width = 1920, height = 1280, label = "5x (1920x1280)" },
};
    }

    // ============ 언어 ============

    public void SetLanguage(string localeCode)
    {
        Locale target = null;
        foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
        {
            if (loc.Identifier.Code == localeCode) { target = loc; break; }
        }
        if (target == null) return;

        LocalizationSettings.SelectedLocale = target;
        ApplyFontForCurrentLocale();

        // GameManager의 OnStatsChanged도 트리거 (HUD 등 갱신)
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyStatsChanged();

        OnLanguageChanged?.Invoke();
    }

    public string GetCurrentLanguageCode()
    {
        var loc = LocalizationSettings.SelectedLocale;
        return loc != null ? loc.Identifier.Code : "ko";
    }

    /// <summary>
    /// 현재 언어에 매핑된 폰트를 씬의 모든 TextMeshPro/UGUI에 강제 적용.
    /// "한 문장 안에서 글씨체 섞임" 버그 해결.
    /// </summary>
    public void ApplyFontForCurrentLocale()
    {
        TMP_FontAsset font = GetFontForLocale(GetCurrentLanguageCode());
        if (font == null) return;

        // UI 텍스트 (TextMeshProUGUI)
        var uguis = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var t in uguis)
        {
            if (t == null) continue;
            // Hidden/Don't save 객체는 제외
            if ((t.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
            // 프리팹 에셋은 제외 (씬에 있는 것만)
            if (!t.gameObject.scene.IsValid()) continue;
            t.font = font;
            t.havePropertiesChanged = true;
        }

        // 월드 텍스트 (TextMeshPro)
        var worldTmps = Resources.FindObjectsOfTypeAll<TextMeshPro>();
        foreach (var t in worldTmps)
        {
            if (t == null) continue;
            if ((t.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            t.font = font;
            t.havePropertiesChanged = true;
        }
    }

    public TMP_FontAsset GetFontForLocale(string code)
    {
        foreach (var pair in languageFonts)
        {
            if (pair.localeCode == code) return pair.fontAsset;
        }
        return null;
    }

    // ============ 사이즈 ============

    public int GetWidgetWidth() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentWidgetW : 384;
    public int GetWidgetHeight() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentWidgetH : 256;
    public int GetStationWidth() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentStationW : 768;
    public int GetStationHeight() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentStationH : 512;

    public void SetWidgetSize(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= sizePresets.Count) return;
        var p = sizePresets[presetIndex];
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.SetWidgetSize(p.width, p.height);
    }

    public void SetStationSize(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= sizePresets.Count) return;
        var p = sizePresets[presetIndex];
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.SetStationSize(p.width, p.height);
    }

    /// <summary>
    /// 현재 widget 사이즈와 일치하는 프리셋 인덱스. 없으면 0.
    /// </summary>
    public int FindPresetIndexForWidget()
    {
        int w = GetWidgetWidth(), h = GetWidgetHeight();
        for (int i = 0; i < sizePresets.Count; i++)
            if (sizePresets[i].width == w && sizePresets[i].height == h) return i;
        return 0;
    }

    public int FindPresetIndexForStation()
    {
        int w = GetStationWidth(), h = GetStationHeight();
        for (int i = 0; i < sizePresets.Count; i++)
            if (sizePresets[i].width == w && sizePresets[i].height == h) return i;
        return 0;
    }
}
