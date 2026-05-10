using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class WindowStateManager : MonoBehaviour
{
    public static WindowStateManager Instance { get; private set; }

    public enum WindowState { Traveling, Transitioning, Docked, TravelingExpanded }

    [Header("Window Sizes (Defaults)")]
    public int widgetWidth = 384;
    public int widgetHeight = 256;
    public int stationWidth = 768;
    public int stationHeight = 512;
    public bool alwaysOnTop = true;

    public int CurrentWidgetW { get; private set; }
    public int CurrentWidgetH { get; private set; }
    public int CurrentStationW { get; private set; }
    public int CurrentStationH { get; private set; }

    [Header("Scene References")]
    public GameObject travelingUI;
    public GameObject dockedUI;
    public GameObject shipObject;
    public ParallaxStarfield starfield;
    public PixelTerrainGenerator terrain;
    public DestinationVisual destinationVisual;

    [Header("Ship Positions")]
    public Vector3 shipDockedPos = new Vector3(-1f, -1.8f, 0);
    public Vector3 shipTravelPos = new Vector3(-3f, 0, 0);

    [Header("Transition Settings")]
    public float arrivalTransitionTime = 1.5f;
    public float dockedFadeInTime = 0.8f;
    public float shipFadeTime = 0.3f;

    [Header("State")]
    public WindowState currentState = WindowState.Traveling;

    [HideInInspector] public StarData dockedStar;

    public event Action<WindowState> OnStateChanged;

    private ShipController shipController;
    private ParticleSystem shipFlame;
    private SpriteRenderer shipRenderer;
    private CanvasGroup dockedCanvasGroup;

    const string PREF_WIDGET_W = "WindowWidgetW";
    const string PREF_WIDGET_H = "WindowWidgetH";
    const string PREF_STATION_W = "WindowStationW";
    const string PREF_STATION_H = "WindowStationH";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")] static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    struct MARGINS { public int left, right, top, bottom; }
    struct RECT { public int left, top, right, bottom; }
    struct POINT { public int x, y; }

    const int GWL_EXSTYLE = -20;
    const int GWL_STYLE = -16;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    const uint WS_MINIMIZEBOX = 0x00020000;
    const uint WS_SYSMENU = 0x00080000;
    const uint WS_EX_TOPMOST = 0x00000008;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_APPWINDOW = 0x00040000;
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_NOSIZE = 0x0001;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private IntPtr hWnd;

    private bool isDragging;
    private POINT dragStartCursor;
    private RECT dragStartWindow;
#endif

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        LoadSizesFromPrefs();

        if (shipObject != null)
        {
            shipController = shipObject.GetComponent<ShipController>();
            shipFlame = shipObject.GetComponentInChildren<ParticleSystem>();
            shipRenderer = shipObject.GetComponent<SpriteRenderer>();
        }

        if (dockedUI != null)
        {
            dockedCanvasGroup = dockedUI.GetComponent<CanvasGroup>();
            if (dockedCanvasGroup == null)
                dockedCanvasGroup = dockedUI.AddComponent<CanvasGroup>();
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        hWnd = GetActiveWindow();
        InitializeWindow();
#endif

        Application.runInBackground = true;
    }

    void LoadSizesFromPrefs()
    {
        CurrentWidgetW = PlayerPrefs.GetInt(PREF_WIDGET_W, widgetWidth);
        CurrentWidgetH = PlayerPrefs.GetInt(PREF_WIDGET_H, widgetHeight);
        CurrentStationW = PlayerPrefs.GetInt(PREF_STATION_W, stationWidth);
        CurrentStationH = PlayerPrefs.GetInt(PREF_STATION_H, stationHeight);
    }

    public void SetWidgetSize(int w, int h)
    {
        CurrentWidgetW = w;
        CurrentWidgetH = h;
        PlayerPrefs.SetInt(PREF_WIDGET_W, w);
        PlayerPrefs.SetInt(PREF_WIDGET_H, h);
        PlayerPrefs.Save();

        if (currentState == WindowState.Traveling)
            ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);
    }

    public void SetStationSize(int w, int h)
    {
        CurrentStationW = w;
        CurrentStationH = h;
        PlayerPrefs.SetInt(PREF_STATION_W, w);
        PlayerPrefs.SetInt(PREF_STATION_H, h);
        PlayerPrefs.Save();

        if (currentState == WindowState.Docked || currentState == WindowState.TravelingExpanded)
            ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void InitializeWindow()
    {
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE | WS_MINIMIZEBOX | WS_SYSMENU);
        uint exStyle = WS_EX_LAYERED | WS_EX_APPWINDOW;
        if (alwaysOnTop) exStyle |= WS_EX_TOPMOST;
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);

        MARGINS margins = new MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0, 0, 0, 0);
    }
#endif

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.isDocked)
        {
            var arrived = GameManager.Instance.arrivedStars;
            if (arrived.Count > 0)
                dockedStar = StarDatabase.Stars[arrived[arrived.Count - 1]];
            SetDockedImmediate();
        }
        else
        {
            SetTraveling();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStarArrived += OnStarArrived;
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        HandleDrag();
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI 위에서 클릭 시작한 경우는 드래그 무시 (드롭다운, 버튼, 패널 등)
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            isDragging = true;
            GetCursorPos(out dragStartCursor);
            GetWindowRect(hWnd, out dragStartWindow);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            POINT currentCursor;
            GetCursorPos(out currentCursor);

            int dx = currentCursor.x - dragStartCursor.x;
            int dy = currentCursor.y - dragStartCursor.y;

            if (Mathf.Abs(dx) > 3 || Mathf.Abs(dy) > 3)
            {
                int newX = dragStartWindow.left + dx;
                int newY = dragStartWindow.top + dy;

                IntPtr insertAfter = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
                SetWindowPos(hWnd, insertAfter, newX, newY, 0, 0, SWP_NOSIZE | SWP_SHOWWINDOW);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
#endif

    void OnStarArrived(StarData star)
    {
        dockedStar = star;
        StartCoroutine(ArrivalTransition());
    }

    public void Depart()
    {
        dockedStar = null;
        if (GameManager.Instance != null)
            GameManager.Instance.DepartToNextStar();
        SetTraveling();
    }

    // ============ 도감/친구창/설정 확장 ============

    public void ExpandForCollection()
    {
        if (currentState != WindowState.Traveling) return;

        currentState = WindowState.TravelingExpanded;
        ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);

        StartCoroutine(FadeShip(fadeOut: true));
        OnStateChanged?.Invoke(currentState);
    }

    public void CollapseFromCollection()
    {
        if (currentState != WindowState.TravelingExpanded) return;

        currentState = WindowState.Traveling;
        ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);

        if (CatManager.Instance != null)
            CatManager.Instance.ApplyCurrentCat();

        StartCoroutine(FadeShip(fadeOut: false));
        OnStateChanged?.Invoke(currentState);
    }

    IEnumerator FadeShip(bool fadeOut)
    {
        if (shipRenderer == null) yield break;

        float startA = shipRenderer.color.a;
        float endA = fadeOut ? 0f : 1f;
        float t = 0f;

        while (t < shipFadeTime)
        {
            t += Time.deltaTime;
            Color c = shipRenderer.color;
            c.a = Mathf.Lerp(startA, endA, t / shipFadeTime);
            shipRenderer.color = c;
            yield return null;
        }

        Color final = shipRenderer.color;
        final.a = endA;
        shipRenderer.color = final;
    }

    // ============ 도착 트랜지션 ============

    IEnumerator ArrivalTransition()
    {
        currentState = WindowState.Transitioning;

        Vector3 targetPos = new Vector3(3f, 0, 0);
        Vector3 startPos = shipObject != null ? shipObject.transform.localPosition : shipTravelPos;
        float elapsed = 0f;

        while (elapsed < arrivalTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arrivalTransitionTime);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            if (shipObject != null)
                shipObject.transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);

            if (shipRenderer != null && t > 0.4f)
            {
                float fadeT = Mathf.InverseLerp(0.4f, 1f, t);
                Color c = shipRenderer.color;
                c.a = 1f - fadeT;
                shipRenderer.color = c;
            }

            if (shipFlame != null)
            {
                var emission = shipFlame.emission;
                emission.rateOverTime = Mathf.Lerp(15f, 0f, t);
            }

            yield return null;
        }

        if (shipRenderer != null)
        {
            Color c = shipRenderer.color;
            c.a = 0f;
            shipRenderer.color = c;
        }

        yield return new WaitForSeconds(0.3f);

        SetDockedImmediate();

        if (dockedCanvasGroup != null)
        {
            dockedCanvasGroup.alpha = 0f;
            float fadeElapsed = 0f;
            while (fadeElapsed < dockedFadeInTime)
            {
                fadeElapsed += Time.deltaTime;
                dockedCanvasGroup.alpha = Mathf.Clamp01(fadeElapsed / dockedFadeInTime);
                yield return null;
            }
            dockedCanvasGroup.alpha = 1f;
        }
    }

    void SetTraveling()
    {
        currentState = WindowState.Traveling;

        if (travelingUI != null) travelingUI.SetActive(true);
        if (dockedUI != null) dockedUI.SetActive(false);

        if (shipObject != null)
        {
            shipObject.SetActive(true);
            if (shipRenderer != null)
            {
                Color c = shipRenderer.color;
                c.a = 1f;
                shipRenderer.color = c;
            }
            if (shipController != null)
            {
                shipController.enabled = true;
                shipController.ResetBasePosition(shipTravelPos);
            }
            else
            {
                shipObject.transform.localPosition = shipTravelPos;
            }
        }
        if (shipFlame != null) shipFlame.Play();
        if (starfield != null) starfield.enabled = true;
        if (terrain != null) terrain.Hide();
        if (destinationVisual != null)
            destinationVisual.gameObject.SetActive(true);

        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.gameObject.SetActive(true);

        ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);
        Camera.main.backgroundColor = HexColor("#06060F");

        OnStateChanged?.Invoke(currentState);
    }

    void SetDockedImmediate()
    {
        currentState = WindowState.Docked;

        if (travelingUI != null) travelingUI.SetActive(false);
        if (dockedUI != null) dockedUI.SetActive(true);

        if (shipObject != null)
        {
            shipObject.SetActive(true);
            shipObject.transform.localPosition = shipDockedPos;
            if (shipRenderer != null)
            {
                Color c = shipRenderer.color;
                c.a = 1f;
                shipRenderer.color = c;
            }
            if (shipController != null)
                shipController.enabled = false;
        }
        if (shipFlame != null) shipFlame.Stop();
        if (starfield != null) starfield.enabled = false;
        if (terrain != null && dockedStar != null)
            terrain.GenerateTerrain(dockedStar);
        if (destinationVisual != null)
            destinationVisual.gameObject.SetActive(false);

        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.gameObject.SetActive(false);

        ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);

        if (dockedStar != null)
        {
            Color bg = dockedStar.color * 0.08f;
            bg.a = 1f;
            Camera.main.backgroundColor = bg;
        }

        OnStateChanged?.Invoke(currentState);
    }

    void ResizeWindow(int w, int h, bool anchorBottomRight)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        int x, y;
        if (anchorBottomRight)
        {
            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);
            x = screenW - w - 16;
            y = screenH - h - 48;
        }
        else
        {
            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);
            x = (screenW - w) / 2;
            y = (screenH - h) / 2;
        }
        IntPtr insertAfter = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(hWnd, insertAfter, x, y, w, h, SWP_SHOWWINDOW);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);  // ← 이 한 줄 추가
#else
        Screen.SetResolution(w, h, false);
#endif
    }

    public void SetAlwaysOnTop(bool value)
    {
        alwaysOnTop = value;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr insertAfter = value ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0, 0x0001 | 0x0002);
#endif
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStarArrived -= OnStarArrived;
    }
}