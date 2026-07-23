using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class WindowStateManager : MonoBehaviour
{
    public static WindowStateManager Instance { get; private set; }

    public enum WindowState { Traveling, Transitioning, Docked, TravelingExpanded, Intro }

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

    [Header("Intro Overlay Objects (인트로 동안만 표시)")]
    [Tooltip("인트로(지구 출발) 동안 켜지고, 끝나면 꺼지는 오브젝트 1")]
    public GameObject introOverlay1;
    [Tooltip("인트로(지구 출발) 동안 켜지고, 끝나면 꺼지는 오브젝트 2")]
    public GameObject introOverlay2;
    [Tooltip("인트로 오버레이 깜빡임 주기(초). 켜짐/꺼짐 각각 이 시간만큼")]
    public float introBlinkInterval = 0.5f;
    [Tooltip("인트로 오버레이 총 깜빡임 시간(초). 이후 꺼진 채 유지")]
    public float introBlinkDuration = 10f;
    private bool introBlinkActive;

    [Header("Intro Settings (첫 실행 시)")]
    [Tooltip("지구 출발 인트로 - 수직 이륙 단계 시간")]
    public float introLaunchDuration = 1.5f;
    [Tooltip("이륙 후 항해 모드 전환 + 지구 점점 작아지는 시간 (10초 권장 - 웅장하게)")]
    public float introFadeOutDuration = 10.0f;
    [Tooltip("이륙 시 ship이 위로 올라가는 거리 (월드 단위)")]
    public float introLaunchHeight = 10f;
    [Tooltip("디버그: 매 실행마다 인트로 강제 재생 (hasSeenIntro 무시)")]
    public bool forceIntroEveryRun = false;

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
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
    const int SW_RESTORE = 9;
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

    IEnumerator StartupRoutine()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0, 0, 0, 0);
        }

        ApplyPopupStyle();
        SetWindowPos(hWnd, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE);

        yield return null;
        yield return null;
#endif
        windowInitialized = true;

        bool shouldPlayIntro = GameManager.Instance != null
            && (forceIntroEveryRun || !GameManager.Instance.hasSeenIntro);

        if (GameManager.Instance != null)
        {
            Debug.Log($"[Intro Check] hasSeenIntro={GameManager.Instance.hasSeenIntro} " +
                $"force={forceIntroEveryRun} → shouldPlayIntro={shouldPlayIntro}");
        }

        if (shouldPlayIntro)
        {
            yield return StartCoroutine(PlayIntroSequence());
        }
        else if (GameManager.Instance != null && GameManager.Instance.isDocked)
        {
            var arrived = GameManager.Instance.arrivedStars;
            if (arrived.Count > 0)
                dockedStar = StarDatabase.GetStar(arrived[arrived.Count - 1]);
            SetDockedImmediate();
        }
        else
        {
            SetTraveling();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnStarArrived += OnStarArrived;
    }

    // ============ 인트로 (첫 실행: 지구 출발) ============

    IEnumerator PlayIntroSequence()
    {
        currentState = WindowState.Intro;

        // 인트로 오버레이 오브젝트 2개 깜빡임 시작 (인트로 끝나면 정지+숨김)
        if (introOverlay1 != null) introOverlay1.SetActive(true);
        if (introOverlay2 != null) introOverlay2.SetActive(true);
        introBlinkActive = true;
        StartCoroutine(BlinkIntroOverlays());

        // 카메라 줌 비활성 - 인트로 중 휠 굴리면 표면 짤림
        CameraZoomController zoom = null;
        if (Camera.main != null)
            zoom = Camera.main.GetComponent<CameraZoomController>();
        if (zoom != null)
        {
            zoom.ResetZoom();
            zoom.enabled = false;
        }

        // 인트로 시작 — 게임 상태 리셋
        if (GameManager.Instance != null)
        {
            GameManager.Instance.distance = 0;
            GameManager.Instance.currentStarIndex = 0;
            GameManager.Instance.isDocked = false;
            GameManager.Instance.arrivedStars.Clear();
        }

        StarData earth = CreateEarthStarData();
        dockedStar = earth;

        if (travelingUI != null) travelingUI.SetActive(false);
        if (dockedUI != null) dockedUI.SetActive(false);

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
        if (shipFlame != null) shipFlame.Play();
        if (starfield != null) starfield.enabled = false;
        if (terrain != null)
        {
            terrain.earthScrollSpeed = 4f; // 표면이 왼쪽으로 흐름 (출발 느낌)
            terrain.GenerateTerrain(earth);
        }
        if (destinationVisual != null)
            destinationVisual.gameObject.SetActive(false);
        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.gameObject.SetActive(false);

        ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);
        Camera.main.backgroundColor = new Color(0.5f, 0.75f, 0.95f); // 지구 하늘색

        yield return new WaitForSeconds(0.8f);

        // === 2단계: 수직 이륙 ===
        bool boostActivated = false;
        if (BoosterSystem.Instance != null)
        {
            BoosterSystem.Instance.gameObject.SetActive(true);
            BoosterSystem.Instance.fuel = 1f;
            BoosterSystem.Instance.isBoosting = true;
            boostActivated = true;
        }

        // 이륙 중엔 표면 스크롤 더 빠르게 (가속 느낌)
        if (terrain != null) terrain.earthScrollSpeed = 12f;

        Vector3 launchStart = shipDockedPos;
        Vector3 launchEnd = launchStart + new Vector3(0, introLaunchHeight, 0);

        float t = 0f;
        while (t < introLaunchDuration)
        {
            t += Time.deltaTime;
            float p = t / introLaunchDuration;
            float eased = p * p;
            if (shipObject != null)
                shipObject.transform.localPosition = Vector3.Lerp(launchStart, launchEnd, eased);
            yield return null;
        }

        if (boostActivated && BoosterSystem.Instance != null)
        {
            BoosterSystem.Instance.isBoosting = false;
            BoosterSystem.Instance.fuel = 0f;
        }

        // === 3단계: 항해 모드 전환 ===
        dockedStar = null;

        currentState = WindowState.Traveling;

        // travelingUI 활성 - 페이드인 위해 일단 alpha 0으로
        CanvasGroup travelingCG = null;
        if (travelingUI != null)
        {
            travelingUI.SetActive(true);
            travelingCG = travelingUI.GetComponent<CanvasGroup>();
            if (travelingCG == null)
                travelingCG = travelingUI.AddComponent<CanvasGroup>();
            travelingCG.alpha = 0f;
        }
        if (dockedUI != null) dockedUI.SetActive(false);

        if (terrain != null)
        {
            terrain.earthScrollSpeed = 0f;
            terrain.Hide();
        }

        if (shipObject != null)
        {
            shipObject.transform.localPosition = shipTravelPos;
            if (shipController != null)
            {
                shipController.enabled = true;
                shipController.ResetBasePosition(shipTravelPos);
            }
        }

        if (starfield != null) starfield.enabled = true;
        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.gameObject.SetActive(true);

        ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);
        Camera.main.backgroundColor = HexColor("#06060F");

        ResetCameraZoom();

        if (destinationVisual != null)
        {
            destinationVisual.gameObject.SetActive(true);
            destinationVisual.ShowAsDeparture(earth, introFadeOutDuration);
        }

        OnStateChanged?.Invoke(currentState);

        // 우주 전환 직후 줌 복원
        if (zoom != null)
            zoom.enabled = true;

        // travelingUI 페이드인 (1.5초)
        if (travelingCG != null)
        {
            float fadeT = 0f;
            float fadeDur = 1.5f;
            while (fadeT < fadeDur)
            {
                fadeT += Time.deltaTime;
                travelingCG.alpha = Mathf.Clamp01(fadeT / fadeDur);
                yield return null;
            }
            travelingCG.alpha = 1f;
        }

        yield return new WaitForSeconds(introFadeOutDuration);

        if (destinationVisual != null)
            destinationVisual.EndDepartureMode();

        // 인트로 오버레이 깜빡임 정지 + 숨김
        introBlinkActive = false;
        if (introOverlay1 != null) introOverlay1.SetActive(false);
        if (introOverlay2 != null) introOverlay2.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.MarkIntroSeen();
    }

    StarData CreateEarthStarData()
    {
        return new StarData
        {
            nameKey = "star_earth", // PixelTerrainGenerator가 이 키로 지구 표면 분기
            typeKey = "type_terrestrial",
            distanceKM = 0,
            color = HexColor("#3D7EFF"),
            reward = 0,
        };
    }

    // ============ 기존 메서드들 ============

    private bool wasMinimized;

    // 인트로 오버레이 점멸 (통째로 on/off). 지정 시간 후 꺼진 상태로 종료.
    IEnumerator BlinkIntroOverlays()
    {
        bool on = true;
        float elapsed = 0f;
        while (introBlinkActive && elapsed < introBlinkDuration)
        {
            on = !on;
            if (introOverlay1 != null) introOverlay1.SetActive(on);
            if (introOverlay2 != null) introOverlay2.SetActive(on);
            yield return new WaitForSeconds(introBlinkInterval);
            elapsed += introBlinkInterval;
        }
        // 점멸 종료 — 켜진 상태로 고정 (인트로 끝나면 그때 숨겨짐)
        introBlinkActive = false;
        if (introOverlay1 != null) introOverlay1.SetActive(true);
        if (introOverlay2 != null) introOverlay2.SetActive(true);
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        HandleDrag();
        DetectRestoreFromMinimize();
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    /// <summary>
    /// 최소화 → 복원 감지. 최소화 중에 정박/항해 상태가 바뀌면 리사이즈가 무시되므로,
    /// 복원 시점에 현재 상태에 맞는 크기로 강제 재적용.
    /// </summary>
    void DetectRestoreFromMinimize()
    {
        if (hWnd == IntPtr.Zero || !windowInitialized) return;

        bool isMinimized = IsIconic(hWnd);

        // 방금 복원됨 (이전엔 minimized, 지금은 아님)
        if (wasMinimized && !isMinimized)
        {
            // 현재 상태에 맞는 크기로 재리사이즈
            if (currentState == WindowState.Docked)
            {
                ResizeWindow(CurrentStationW, CurrentStationH, anchorBottomRight: true);
            }
            else if (currentState == WindowState.Traveling)
            {
                ResizeWindow(CurrentWidgetW, CurrentWidgetH, anchorBottomRight: true);
            }
        }

        wasMinimized = isMinimized;
    }
#endif

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

        // 도착 직전 카메라 줌 + 위치 즉시 리셋 — 줌인 상태에서 도착하면 화면 짤림
        ResetCameraZoom();

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

        // 카메라 줌 + 위치 즉시 리셋 (정박 중 줌이 변경됐을 수 있어도 안전하게)
        ResetCameraZoom();

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

        // 카메라 즉시 리셋 (줌 인 상태에서 정박하면 화면 짤림)
        ResetCameraZoom();

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
    IEnumerator ResizeWindowRoutine(int w, int h, bool anchorBottomRight)
    {
        // 최소화 상태면 먼저 복원 (안 그러면 SetWindowPos가 무시됨)
        if (hWnd != IntPtr.Zero && IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
            yield return null;
            yield return null;
        }

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

        ApplyPopupStyle();
        Screen.SetResolution(w, h, false);

        yield return null;
        yield return null;

        IntPtr insertAfter = alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(hWnd, insertAfter, x, y, w, h, SWP_SHOWWINDOW);
        ApplyPopupStyle();
        SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE);

        yield return null;
        RECT clientRect;
        if (GetClientRect(hWnd, out clientRect))
        {
            int clientW = clientRect.right - clientRect.left;
            int clientH = clientRect.bottom - clientRect.top;
            if (clientW != w || clientH != h)
            {
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
