using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class WindowStateManager : MonoBehaviour
{
    public static WindowStateManager Instance { get; private set; }

    public enum WindowState { Traveling, Transitioning, Docked, TravelingExpanded }

    [Header("Base Sizes (1920x1080 기준)")]
    public int widgetWidth = 384;
    public int widgetHeight = 256;
    public int stationWidth = 768;
    public int stationHeight = 512;
    public bool alwaysOnTop = true;

    public int CurrentWidgetW { get; private set; }
    public int CurrentWidgetH { get; private set; }
    public int CurrentStationW { get; private set; }
    public int CurrentStationH { get; private set; }

    const int BASE_SCREEN_W = 1920;

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

    private bool windowInitialized;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")] static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
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

    const uint SWP_NOSIZE       = 0x0001;
    const uint SWP_NOMOVE       = 0x0002;
    const uint SWP_NOZORDER     = 0x0004;
    const uint SWP_NOACTIVATE   = 0x0010;
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_SHOWWINDOW   = 0x0040;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private IntPtr hWnd;
    private Coroutine resizeRoutine;

    private bool isDragging;
    private POINT dragStartCursor;
    private RECT dragStartWindow;
#endif

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        CalculateSizes();

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

        // ★ 핵심: SetResolution 호출되기 전에 popup 스타일을 먼저 적용.
        // 이러면 Unity가 이전 실행 사이즈로 standard 윈도우를 만들었더라도
        // 즉시 popup으로 전환되어 caption/border가 제거됨 → 외곽=client.
        ApplyPopupStyle();
#endif

        Application.runInBackground = true;
    }

    void CalculateSizes()
    {
        int monitorW = GetMonitorWidth();
        if (monitorW <= 0) monitorW = BASE_SCREEN_W;

        float scale = (float)monitorW / BASE_SCREEN_W;
        if (scale < 0.5f) scale = 0.5f;

        CurrentWidgetW = Mathf.RoundToInt(widgetWidth * scale);
        CurrentWidgetH = Mathf.RoundToInt(widgetHeight * scale);
        CurrentStationW = Mathf.RoundToInt(stationWidth * scale);
        CurrentStationH = Mathf.RoundToInt(stationHeight * scale);
    }

    int GetMonitorWidth()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        return GetSystemMetrics(0);
#else
        if (Display.main != null && Display.main.systemWidth > 0)
            return Display.main.systemWidth;
        return Screen.currentResolution.width;
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void ApplyPopupStyle()
    {
        SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE | WS_MINIMIZEBOX | WS_SYSMENU);
        uint exStyle = WS_EX_LAYERED | WS_EX_APPWINDOW;
        if (alwaysOnTop) exStyle |= WS_EX_TOPMOST;
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);

        MARGINS margins = new MARGINS { left = -1, right = -1, top = -1, bottom = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
    }
#endif

    void Start()
    {
        StartCoroutine(StartupRoutine());
    }

    /// <summary>
    /// 초기화 race condition + Unity가 이전 실행 사이즈를 기억하는 문제를 동시에 해결.
    ///
    /// Awake에서 이미 popup 스타일을 적용했으므로 윈도우 외곽=client area가 보장됨.
    /// 이제 Unity backbuffer가 안정화되길 기다린 뒤 카메라 설정 + 상태 전환 진입.
    /// </summary>
    IEnumerator StartupRoutine()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // 카메라 투명 배경 설정
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0, 0, 0, 0);
        }

        // 한 번 더 popup 스타일 + frame change 확정
        ApplyPopupStyle();
        SetWindowPos(hWnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE);

        yield return null;
        yield return null;
#endif
        windowInitialized = true;

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

    void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (hasFocus && alwaysOnTop && windowInitialized)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
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

    public void ExpandForCollection()
    {
        if (currentState != WindowState.Traveling) return;

        currentState = WindowState.TravelingExpanded;
        ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);

        OnStateChanged?.Invoke(currentState);
    }

    public void CollapseFromCollection()
    {
        if (currentState != WindowState.TravelingExpanded) return;

        currentState = WindowState.Traveling;
        ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);

        if (CatManager.Instance != null)
            CatManager.Instance.ApplyCurrentCat();

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

    IEnumerator ArrivalTransition()
    {
        currentState = WindowState.Transitioning;

        Vector3 targetPos = new Vector3(3f, 0, 0);
        Vector3 startPos = shipObject != null ? shipObject.transform.localPosition : Vector3.zero;

        float t = 0f;
        while (t < arrivalTransitionTime)
        {
            t += Time.deltaTime;
            if (shipObject != null)
                shipObject.transform.localPosition = Vector3.Lerp(startPos, targetPos, t / arrivalTransitionTime);
            yield return null;
        }

        SetDockedImmediate();

        if (dockedCanvasGroup != null)
        {
            dockedCanvasGroup.alpha = 0f;
            float t2 = 0f;
            while (t2 < dockedFadeInTime)
            {
                t2 += Time.deltaTime;
                dockedCanvasGroup.alpha = t2 / dockedFadeInTime;
                yield return null;
            }
            dockedCanvasGroup.alpha = 1f;
        }
    }

    void ResetCameraZoom()
    {
        if (Camera.main == null) return;
        var zoom = Camera.main.GetComponent<CameraZoomController>();
        if (zoom != null) zoom.ResetZoom();
    }

    public void SetTraveling()
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

        ResetCameraZoom();
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

        ResetCameraZoom();
        OnStateChanged?.Invoke(currentState);
    }

    void ResizeWindow(int w, int h, bool anchorBottomRight)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (resizeRoutine != null) StopCoroutine(resizeRoutine);
        resizeRoutine = StartCoroutine(ResizeWindowRoutine(w, h, anchorBottomRight));
#else
        Screen.SetResolution(w, h, false);
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    /// <summary>
    /// 1. popup 스타일 먼저 강제 (외곽=client 보장)
    /// 2. SetResolution
    /// 3. 2프레임 대기 (Unity backbuffer 안정화)
    /// 4. 위치/크기/z-order 설정
    /// 5. popup 스타일 + topmost 재확정
    /// 6. client area 검증, 어긋났으면 외곽 크기 보정 (방어 코드)
    /// </summary>
    IEnumerator ResizeWindowRoutine(int w, int h, bool anchorBottomRight)
    {
        int screenW = GetSystemMetrics(0);
        int screenH = GetSystemMetrics(1);
        int x, y;
        if (anchorBottomRight)
        {
            x = screenW - w - 16;
            y = screenH - h - 48;
        }
        else
        {
            x = (screenW - w) / 2;
            y = (screenH - h) / 2;
        }

        // Step 1: SetResolution 직전에 popup 스타일 적용 → 외곽=client 보장
        ApplyPopupStyle();

        // Step 2: backbuffer 변경
        Screen.SetResolution(w, h, false);

        // Step 3: Unity backbuffer 안정화 대기
        yield return null;
        yield return null;

        IntPtr insertAfter = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;

        // Step 4: 위치/크기/z-order
        SetWindowPos(hWnd, insertAfter, x, y, w, h, SWP_SHOWWINDOW);

        // Step 5: popup 스타일 + frame change 재확정 (Unity가 리셋했을 가능성)
        ApplyPopupStyle();
        SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE);

        // Step 6: client area 검증. 어긋났으면 한 번 더 강제로 외곽 사이즈 재설정.
        // popup이 제대로 적용됐다면 client == 외곽이지만, Unity가 캐싱한 standard
        // 스타일이 남아있다면 client < 외곽이 되어 비율이 깨짐.
        yield return null;
        RECT clientRect;
        if (GetClientRect(hWnd, out clientRect))
        {
            int clientW = clientRect.right - clientRect.left;
            int clientH = clientRect.bottom - clientRect.top;
            if (clientW != w || clientH != h)
            {
                // popup 스타일 다시 강제 + 외곽 크기 재설정으로 복구
                ApplyPopupStyle();
                SetWindowPos(hWnd, insertAfter, x, y, w, h,
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            }
        }

        if (Camera.main != null) Camera.main.ResetAspect();

        resizeRoutine = null;
    }
#endif

    public void SetAlwaysOnTop(bool value)
    {
        alwaysOnTop = value;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ApplyPopupStyle();
        SetWindowPos(hWnd, value ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE);
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