using System;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

/// <summary>
/// 창 컨트롤 버튼 핸들러. 최소화/종료/도감/친구창/설정.
/// 각 버튼 GameObject에 붙이고 Button Type을 Inspector에서 선택.
///
/// 도감/친구/설정 셋 중 하나가 열려있으면 나머지 둘은 비활성화 →
/// 한 번에 한 패널만 열리도록 강제.
/// </summary>
public class WindowButton : MonoBehaviour
{
    public enum ButtonType { Minimize, Close, Collection, Friends, Settings }

    public ButtonType buttonType;

    [Header("Collection Panel (Collection button only)")]
    public GameObject collectionPanel;

    [Header("Friends Panel (Friends button only)")]
    public GameObject friendsPanel;

    [Header("Settings Panel (Settings button only)")]
    public GameObject settingsPanel;

    [Header("Cross-references (모든 도감/친구/설정 버튼에 연결)")]
    [Tooltip("다른 패널과의 동시 오픈 방지를 위해, 도감/친구/설정 패널 셋 다 연결해두면 됨")]
    public GameObject otherPanelA;
    public GameObject otherPanelB;

    private Button button;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    const int SW_MINIMIZE = 6;
#endif

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        // 도감/친구/설정 버튼만 동시 오픈 방지 체크
        if (buttonType != ButtonType.Collection
            && buttonType != ButtonType.Friends
            && buttonType != ButtonType.Settings)
            return;

        if (button == null) return;

        // 다른 패널 중 하나라도 열려있으면 비활성
        bool blocked = (otherPanelA != null && otherPanelA.activeSelf)
                    || (otherPanelB != null && otherPanelB.activeSelf);

        button.interactable = !blocked;
    }

    void OnClick()
    {
        switch (buttonType)
        {
            case ButtonType.Minimize:
                Minimize();
                break;
            case ButtonType.Close:
                if (GameManager.Instance != null)
                    GameManager.Instance.SaveGame();
                Application.Quit();
                break;
            case ButtonType.Collection:
                if (IsAnotherPanelOpen()) return;
                if (collectionPanel != null) collectionPanel.SetActive(true);
                break;
            case ButtonType.Friends:
                if (IsAnotherPanelOpen()) return;
                if (friendsPanel != null) friendsPanel.SetActive(true);
                break;
            case ButtonType.Settings:
                if (IsAnotherPanelOpen()) return;
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
        }
    }

    bool IsAnotherPanelOpen()
    {
        if (otherPanelA != null && otherPanelA.activeSelf) return true;
        if (otherPanelB != null && otherPanelB.activeSelf) return true;
        return false;
    }

    void Minimize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr hWnd = GetActiveWindow();
        ShowWindow(hWnd, SW_MINIMIZE);
#endif
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}