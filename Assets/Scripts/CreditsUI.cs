using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크레딧 세부 패널. 일단 placeholder.
/// </summary>
public class CreditsUI : MonoBehaviour
{
    [Header("References")]
    public Button closeButton;

    [Header("Reopen After Close")]
    public SettingsPanelUI settingsPanelToReopen;

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);
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
