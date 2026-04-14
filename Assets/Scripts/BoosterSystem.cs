using UnityEngine;

public class BoosterSystem : MonoBehaviour
{
    public static BoosterSystem Instance { get; private set; }

    [Header("Gauge Settings")]
    public int gaugeWidth = 6;
    public int gaugeHeight = 48;
    public int borderWidth = 1;
    public int pixelsPerUnit = 8;
    public int sortingOrder = 20;

    [Header("Fill Rates")]
    public float autoFillPerSecond = 0.02f;
    public float keyPressFill = 0.03f;

    [Header("Boost Settings")]
    public float boostSpeedMultiplier = 3f;
    public float boostDrainPerSecond = 0.2f;
    public float boostActivateDelay = 0.5f;

    [Header("State")]
    public float fuel;
    public bool isBoosting;
    public bool isFullWaiting;

    private Texture2D gaugeTex;
    private SpriteRenderer sr;
    private Sprite gaugeSprite;   // 한 번만 생성
    private float fullWaitTimer;

    // 갱신 최적화
    private int lastDrawnFuelPixel = -1;
    private bool lastDrawnBoostState;

    private readonly Color borderColor = HexColor("#1a1a2e");
    private readonly Color emptyColor = HexColor("#1a1a28");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CreateGaugeSprite();

        if (GlobalInputHook.Instance != null)
        {
            GlobalInputHook.Instance.OnGlobalKeyPress += OnKeyPress;
            GlobalInputHook.Instance.OnGlobalMouseClick += OnMouseClick;
        }
    }

    void Update()
    {
        if (isBoosting)
        {
            fuel -= boostDrainPerSecond * Time.deltaTime;
            if (fuel <= 0f)
            {
                fuel = 0f;
                isBoosting = false;
            }
        }
        else if (isFullWaiting)
        {
            fullWaitTimer -= Time.deltaTime;
            if (fullWaitTimer <= 0f)
            {
                isFullWaiting = false;
                isBoosting = true;
            }
        }
        else
        {
            fuel += autoFillPerSecond * Time.deltaTime;
            if (fuel >= 1f)
            {
                fuel = 1f;
                isFullWaiting = true;
                fullWaitTimer = boostActivateDelay;
            }
        }

        // 화면상 변화 있을 때만 텍스처 갱신
        int currentFuelPixel = Mathf.FloorToInt(fuel * gaugeHeight);
        if (currentFuelPixel != lastDrawnFuelPixel || isBoosting != lastDrawnBoostState)
        {
            lastDrawnFuelPixel = currentFuelPixel;
            lastDrawnBoostState = isBoosting;
            UpdateGaugeVisual();
        }
    }

    void LateUpdate()
    {
        // 화면 왼쪽 고정
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = new Vector3(10f, Screen.height / 2f, 10f);
        transform.position = cam.ScreenToWorldPoint(screenPos);

        float zoomScale = cam.orthographicSize / 5f;
        transform.localScale = Vector3.one * zoomScale;
    }

    // ============ 입력 ============

    void OnKeyPress()
    {
        // 부스터 사용 중이 아닐 때만 충전
        if (!isBoosting && !isFullWaiting)
        {
            fuel += keyPressFill;
            if (fuel >= 1f)
            {
                fuel = 1f;
                isFullWaiting = true;
                fullWaitTimer = boostActivateDelay;
            }
        }
    }

    void OnMouseClick()
    {
        // 부스터 상태 상관없이 항상 재화 +1
        if (GameManager.Instance != null)
        {
            GameManager.Instance.credits += 1;
            GameManager.Instance.totalCredits += 1;
        }
    }

    // ============ 속도 ============

    public float GetSpeedMultiplier()
    {
        return isBoosting ? boostSpeedMultiplier : 1f;
    }

    // ============ 게이지 비주얼 ============

    void CreateGaugeSprite()
    {
        int totalW = gaugeWidth + borderWidth * 2;
        int totalH = gaugeHeight + borderWidth * 2;

        gaugeTex = new Texture2D(totalW, totalH, TextureFormat.RGBA32, false);
        gaugeTex.filterMode = FilterMode.Point;
        gaugeTex.wrapMode = TextureWrapMode.Clamp;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;

        // Sprite 한 번만 생성
        gaugeSprite = Sprite.Create(
            gaugeTex,
            new Rect(0, 0, totalW, totalH),
            new Vector2(0f, 0.5f),
            pixelsPerUnit
        );
        sr.sprite = gaugeSprite;

        UpdateGaugeVisual();
    }

    void UpdateGaugeVisual()
    {
        if (gaugeTex == null) return;

        int totalW = gaugeWidth + borderWidth * 2;
        int totalH = gaugeHeight + borderWidth * 2;

        Color[] pixels = new Color[totalW * totalH];

        for (int y = 0; y < totalH; y++)
        {
            for (int x = 0; x < totalW; x++)
            {
                if (x < borderWidth || x >= totalW - borderWidth ||
                    y < borderWidth || y >= totalH - borderWidth)
                {
                    if (isBoosting)
                    {
                        float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
                        pixels[y * totalW + x] = Color.Lerp(borderColor, HexColor("#4488ff"), flash);
                    }
                    else if (isFullWaiting)
                    {
                        float flash = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
                        pixels[y * totalW + x] = Color.Lerp(borderColor, HexColor("#ffaa00"), flash);
                    }
                    else
                    {
                        pixels[y * totalW + x] = borderColor;
                    }
                    continue;
                }

                int innerY = y - borderWidth;
                float fillLine = fuel * gaugeHeight;

                if (innerY < fillLine)
                {
                    float t = (float)innerY / gaugeHeight;
                    Color fillColor;
                    if (t < 0.4f)
                        fillColor = Color.Lerp(HexColor("#cc8800"), HexColor("#ffcc00"), t / 0.4f);
                    else if (t < 0.7f)
                        fillColor = Color.Lerp(HexColor("#ffcc00"), HexColor("#ff6600"), (t - 0.4f) / 0.3f);
                    else
                        fillColor = Color.Lerp(HexColor("#ff6600"), HexColor("#3388ff"), (t - 0.7f) / 0.3f);

                    if (isBoosting)
                    {
                        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 8f + innerY * 0.5f);
                        fillColor *= pulse;
                        fillColor.a = 1f;
                    }

                    pixels[y * totalW + x] = fillColor;
                }
                else
                {
                    pixels[y * totalW + x] = emptyColor;
                }
            }
        }

        gaugeTex.SetPixels(pixels);
        gaugeTex.Apply();
        // Sprite 재생성 안 함 — 텍스처만 갱신하면 자동 반영
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    void OnDestroy()
    {
        if (gaugeTex != null) Destroy(gaugeTex);
        if (gaugeSprite != null) Destroy(gaugeSprite);

        if (GlobalInputHook.Instance != null)
        {
            GlobalInputHook.Instance.OnGlobalKeyPress -= OnKeyPress;
            GlobalInputHook.Instance.OnGlobalMouseClick -= OnMouseClick;
        }
    }
}
