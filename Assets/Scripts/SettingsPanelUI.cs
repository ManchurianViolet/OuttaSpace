using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 메인 모달. 화면 중앙에 작게 뜨고 3개 버튼 (언어/크레딧/초기화).
///
/// 세부 패널 열면 자기 자신은 SetActive(false)가 아니라 CanvasGroup으로 숨김.
/// 이유: GameObject가 active 상태여야 WindowButton이 "설정 열려있음"으로 인지하고
/// 다른 버튼들(도감/친구)을 비활성 유지.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button languageButton;
    public Button creditsButton;
    public Button resetButton;
    public Button closeButton;

    [Header("Button Labels (TMP Text inside each button)")]
    public TextMeshProUGUI languageButtonLabel;
    public TextMeshProUGUI creditsButtonLabel;
    public TextMeshProUGUI resetButtonLabel;

    [Header("Sub Panels")]
    public GameObject languagePanel;
    public GameObject creditsPanel;
    public GameObject resetConfirmPanel;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        ShowSelf();

        if (languageButton != null) languageButton.onClick.AddListener(OpenLanguage);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
        if (resetButton != null) resetButton.onClick.AddListener(OpenResetConfirm);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;

        RefreshTexts();
    }

    void OnDisable()
    {
        if (languageButton != null) languageButton.onClick.RemoveListener(OpenLanguage);
        if (creditsButton != null) creditsButton.onClick.RemoveListener(OpenCredits);
        if (resetButton != null) resetButton.onClick.RemoveListener(OpenResetConfirm);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (languageButtonLabel != null) languageButtonLabel.text = Loc.Get("settings_language");
        if (creditsButtonLabel != null) creditsButtonLabel.text = Loc.Get("settings_credits");
        if (resetButtonLabel != null) resetButtonLabel.text = Loc.Get("settings_reset");
    }

    void OpenLanguage()
    {
        if (languagePanel != null) languagePanel.SetActive(true);
        HideSelf();
    }

    void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
        HideSelf();
    }

    void OpenResetConfirm()
    {
        if (resetConfirmPanel != null) resetConfirmPanel.SetActive(true);
        HideSelf();
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 세부 패널이 X 닫힐 때 호출. SettingsPanel 다시 보이게.
    /// </summary>
    public void Reopen()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        ShowSelf();
    }

    void HideSelf()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    void ShowSelf()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
