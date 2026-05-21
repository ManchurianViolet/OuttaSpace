using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 초기화 확인 모달.
/// 설정 → 초기화 클릭 시 열림. 예 누르면 모든 데이터 삭제 + 씬 재로드.
///
/// SettingsPanelUI의 sub panel로 동작. X 또는 No 누르면 부모 다시 보임.
/// </summary>
public class ResetConfirmUI : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI messageLabel;
    public TextMeshProUGUI yesButtonLabel;
    public TextMeshProUGUI noButtonLabel;

    [Header("Buttons")]
    public Button yesButton;
    public Button noButton;
    public Button closeButton;

    [Header("Parent")]
    [Tooltip("닫을 때 다시 보여줄 부모 설정 패널.")]
    public SettingsPanelUI parentSettings;

    void OnEnable()
    {
        RefreshTexts();

        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
        if (closeButton != null) closeButton.onClick.AddListener(OnNo);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged += RefreshTexts;
    }

    void OnDisable()
    {
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYes);
        if (noButton != null) noButton.onClick.RemoveListener(OnNo);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnNo);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnLanguageChanged -= RefreshTexts;
    }

    void RefreshTexts()
    {
        if (titleLabel != null) titleLabel.text = Loc.Get("reset_warning_title");
        if (messageLabel != null) messageLabel.text = Loc.Get("reset_warning_message");
        if (yesButtonLabel != null) yesButtonLabel.text = Loc.Get("confirm_yes");
        if (noButtonLabel != null) noButtonLabel.text = Loc.Get("confirm_no");
    }

    void OnYes()
    {
        // Shift+R 단축키와 동일한 동작
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // GameManager Instance 정리하고 씬 재로드
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void OnNo()
    {
        gameObject.SetActive(false);
        if (parentSettings != null)
            parentSettings.Reopen();
    }
}
