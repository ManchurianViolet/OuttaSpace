using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 설정 통합 관리: 언어, 언어별 폰트.
/// (창 사이즈는 WindowStateManager가 모니터 해상도에 따라 자동 결정)
/// 빈 GameObject에 붙이고 Inspector에서 폰트 매핑 설정.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Language Fonts")]
    [Tooltip("언어 코드별 TMP_FontAsset 매핑. 언어 변경 시 모든 텍스트의 폰트를 강제 교체.")]
    public List<LanguageFontPair> languageFonts = new List<LanguageFontPair>();

    public event Action OnLanguageChanged;

    [System.Serializable]
    public class LanguageFontPair
    {
        public string localeCode;        // "ko", "en"
        public TMP_FontAsset fontAsset;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
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

        // 저장된 언어 복원
        string savedLang = PlayerPrefs.GetString("Language", "");
        if (!string.IsNullOrEmpty(savedLang))
        {
            foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
            {
                if (loc.Identifier.Code == savedLang)
                {
                    LocalizationSettings.SelectedLocale = loc;
                    break;
                }
            }
        }

        ApplyFontForCurrentLocale();
        OnLanguageChanged?.Invoke();
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

        // 언어 저장 (다음 실행 시 복원)
        PlayerPrefs.SetString("Language", localeCode);
        PlayerPrefs.Save();

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

    // ============ 사이즈 (읽기 전용 - WindowStateManager가 자동 결정) ============

    public int GetWidgetWidth() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentWidgetW : 384;
    public int GetWidgetHeight() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentWidgetH : 256;
    public int GetStationWidth() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentStationW : 768;
    public int GetStationHeight() => WindowStateManager.Instance != null
        ? WindowStateManager.Instance.CurrentStationH : 512;
}