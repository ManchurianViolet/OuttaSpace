using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

/// <summary>
/// 창 컨트롤 버튼 핸들러. 최소화/종료/도감 열기.
/// 각 버튼 GameObject에 붙이고 Button Type을 Inspector에서 선택.
/// </summary>
public class WindowButton : MonoBehaviour
{
    public enum ButtonType { Minimize, Close, Collection }

    public ButtonType buttonType;

    [Header("Collection Panel (Collection button only)")]
    public GameObject collectionPanel;

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
                OpenCollection();
                break;
        }
    }

    void Minimize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr hWnd = GetActiveWindow();
        ShowWindow(hWnd, SW_MINIMIZE);
#endif
    }

    void OpenCollection()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(true);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}
