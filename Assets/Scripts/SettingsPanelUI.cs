using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 메인 모달. 화면 중앙에 작게 뜨고 3개 버튼 (언어/크기/크레딧).
/// 세부 패널 열면 자기 자신 숨김 (세부 X 닫으면 다시 보임).
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button languageButton;
    public Button sizeButton;
    public Button creditsButton;
    public Button closeButton;

    [Header("Button Labels (TMP Text inside each button)")]
    public TextMeshProUGUI languageButtonLabel;
    public TextMeshProUGUI sizeButtonLabel;
    public TextMeshProUGUI creditsButtonLabel;

    [Header("Sub Panels")]
    public GameObject languagePanel;
    public GameObject sizePanel;
    public GameObject creditsPanel;

    void OnEnable()
    {
        if (languageButton != null) languageButton.onClick.AddListener(OpenLanguage);
        if (sizeButton != null)     sizeButton.onClick.AddListener(OpenSize);
        if (creditsButton != null)  creditsButton.onClick.AddListener(OpenCredits);
        if (closeButton != null)    closeButton.onClick.AddListener(Close);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;

        RefreshTexts();
    }

    void OnDisable()
    {
        if (languageButton != null) languageButton.onClick.RemoveListener(OpenLanguage);
        if (sizeButton != null)     sizeButton.onClick.RemoveListener(OpenSize);
        if (creditsButton != null)  creditsButton.onClick.RemoveListener(OpenCredits);
        if (closeButton != null)    closeButton.onClick.RemoveListener(Close);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (languageButtonLabel != null) languageButtonLabel.text = Loc.Get("settings_language");
        if (sizeButtonLabel != null)     sizeButtonLabel.text     = Loc.Get("settings_size");
        if (creditsButtonLabel != null)  creditsButtonLabel.text  = Loc.Get("settings_credits");
    }

    void OpenLanguage()
    {
        if (languagePanel != null) languagePanel.SetActive(true);
        gameObject.SetActive(false);
    }

    void OpenSize()
    {
        if (sizePanel != null) sizePanel.SetActive(true);
        gameObject.SetActive(false);
    }

    void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 세부 패널이 X 닫힐 때 호출. SettingsPanel 다시 표시.
    /// </summary>
    public void Reopen()
    {
        gameObject.SetActive(true);
    }
}
