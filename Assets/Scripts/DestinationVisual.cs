using UnityEngine;

public class DestinationVisual : MonoBehaviour
{
    [Header("References")]
    public CameraZoomController zoomController;

    [Header("Position Settings")]
    public float offScreenX = 15f;
    public float arrivedX = 3f;
    public float yPosition = 0f;

    [Header("Scale Settings")]
    public float minScale = 0.1f;
    public float maxScale = 2.5f;

    [Header("Glow")]
    public bool showGlow = true;

    [Header("Departure Mode (인트로 출발지 표시)")]
    [Tooltip("출발지 시작 X 위치 (ship 가까이 = 왼쪽).")]
    public float departureStartX = -4f;
    [Tooltip("출발지 끝 X 위치 (화면 밖 왼쪽으로 사라짐).")]
    public float departureEndX = -22f;
    [Tooltip("출발지 시작 스케일 (크게).")]
    public float departureStartScale = 2.0f;
    [Tooltip("출발지 끝 스케일 (작아지면서 사라짐).")]
    public float departureEndScale = 0.08f;
    [Tooltip("지구 자전 속도 (초당 회전 사이클). 시각적 자전 효과는 텍스처 오프셋으로 표현.")]
    public float earthRotationSpeed = 0.15f;

    private GameObject planetObj;
    private SpriteRenderer planetRenderer;
    private GameObject glowObj;
    private SpriteRenderer glowRenderer;
    private Texture2D planetTex;
    private Texture2D glowTex;
    private int currentDestIndex = -1;

    private bool inDepartureMode;
    private float departureTimer;
    private float departureDuration;
    private Color departurePlanetColor;
    private Texture2D departureTex;
    // 자전 효과용 - 여러 프레임의 지구 텍스처 미리 생성
    private Texture2D[] earthFrames;
    private const int EARTH_FRAME_COUNT = 12;
    private const int EARTH_TEX_SIZE = 32;

    void Start()
    {
        CreatePlanetObject();
        CreateGlowObject();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged += UpdateVisual;
            GameManager.Instance.OnStarArrived += OnArrived;
        }
        UpdateVisual();
    }

    void Update()
    {
        if (inDepartureMode)
        {
            UpdateDeparture();
            return;
        }

        UpdateVisual();
        if (planetObj != null)
        {
            float wobble = Mathf.Sin(Time.time * 0.3f) * 0.05f;
            var pos = planetObj.transform.localPosition;
            planetObj.transform.localPosition = new Vector3(pos.x, yPosition + wobble, 0);
        }
    }

    // ============ 출발지 모드 (인트로용 지구) ============

    public void ShowAsDeparture(StarData departure, float duration)
    {
        inDepartureMode = true;
        departureTimer = 0f;
        departureDuration = duration;
        departurePlanetColor = new Color(0.3f, 0.6f, 1f); // 푸른 지구

        // 지구 자전 프레임 미리 생성
        GenerateEarthFrames();

        if (planetRenderer != null && earthFrames != null && earthFrames.Length > 0)
        {
            planetRenderer.sprite = Sprite.Create(
                earthFrames[0],
                new Rect(0, 0, EARTH_TEX_SIZE, EARTH_TEX_SIZE),
                Vector2.one * 0.5f, 16);
        }

        if (planetObj != null)
        {
            planetObj.SetActive(true);
            planetObj.transform.localPosition = new Vector3(departureStartX, yPosition, 0);
            planetObj.transform.localScale = Vector3.one * departureStartScale;
        }

        if (glowObj != null && showGlow)
        {
            glowObj.SetActive(true);
            glowObj.transform.localPosition = new Vector3(departureStartX, yPosition, 0);
            glowObj.transform.localScale = Vector3.one * departureStartScale * 2.5f;
        }
    }

    void UpdateDeparture()
    {
        departureTimer += Time.deltaTime;
        float t = Mathf.Clamp01(departureTimer / departureDuration);

        // ease-in-out: 처음 천천히 → 중간 빠르게 → 끝 다시 천천히
        float eased = t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2) * 0.5f;

        float x = Mathf.Lerp(departureStartX, departureEndX, eased);
        float scale = Mathf.Lerp(departureStartScale, departureEndScale, eased);

        // 알파 페이드: 70% 지나서야 본격적으로 사라지기 시작
        float alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);

        // 자전 효과: 프레임 순환
        if (planetRenderer != null && earthFrames != null && earthFrames.Length > 0)
        {
            int frameIdx = Mathf.FloorToInt(departureTimer * earthRotationSpeed * EARTH_FRAME_COUNT) % EARTH_FRAME_COUNT;
            if (frameIdx < 0) frameIdx = 0;
            planetRenderer.sprite = Sprite.Create(
                earthFrames[frameIdx],
                new Rect(0, 0, EARTH_TEX_SIZE, EARTH_TEX_SIZE),
                Vector2.one * 0.5f, 16);
        }

        if (planetObj != null)
        {
            planetObj.transform.localPosition = new Vector3(x, yPosition, 0);
            planetObj.transform.localScale = Vector3.one * scale;
            if (planetRenderer != null)
            {
                Color c = planetRenderer.color;
                c.a = alpha;
                planetRenderer.color = c;
            }
        }

        if (glowObj != null)
        {
            glowObj.transform.localPosition = new Vector3(x, yPosition, 0);
            glowObj.transform.localScale = Vector3.one * scale * 2.5f;
            if (glowRenderer != null)
            {
                Color gc = departurePlanetColor;
                gc.a = alpha * 0.3f;
                glowRenderer.color = gc;
            }
        }
    }

    public void EndDepartureMode()
    {
        inDepartureMode = false;
        if (planetObj != null)
        {
            planetObj.SetActive(false);
            if (planetRenderer != null)
            {
                Color c = planetRenderer.color;
                c.a = 1f;
                planetRenderer.color = c;
            }
        }
        if (glowObj != null) glowObj.SetActive(false);

        // 지구 프레임 메모리 해제
        if (earthFrames != null)
        {
            for (int i = 0; i < earthFrames.Length; i++)
                if (earthFrames[i] != null) Destroy(earthFrames[i]);
            earthFrames = null;
        }

        if (departureTex != null)
        {
            Destroy(departureTex);
            departureTex = null;
        }
        currentDestIndex = -1;
    }

    /// <summary>
    /// 지구 자전 애니메이션용 프레임을 미리 생성.
    /// 각 프레임은 longitude offset이 다른 지구 텍스처.
    /// </summary>
    void GenerateEarthFrames()
    {
        if (earthFrames != null) return;

        earthFrames = new Texture2D[EARTH_FRAME_COUNT];
        for (int i = 0; i < EARTH_FRAME_COUNT; i++)
        {
            float longitudeOffset = (float)i / EARTH_FRAME_COUNT;
            earthFrames[i] = GenerateEarth(EARTH_TEX_SIZE, longitudeOffset);
        }
    }

    /// <summary>
    /// 지구 텍스처 생성.
    /// - 둥근 원형
    /// - 파란 바다 베이스
    /// - 초록 대륙 (perlin noise + 위도 영향)
    /// - 양 극 흰색 (북극/남극)
    /// - 흰 구름 점들
    /// - 한쪽이 어두운 그림자 (음영)
    /// </summary>
    Texture2D GenerateEarth(int size, float longitudeOffset)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float radius = (size - 2) * 0.5f;
        float cx = size * 0.5f;
        float cy = size * 0.5f;

        // 지구 색 팔레트
        Color oceanLight = new Color(0.30f, 0.55f, 0.90f);  // 얕은 바다
        Color ocean = new Color(0.15f, 0.40f, 0.78f);       // 깊은 바다
        Color oceanDark = new Color(0.08f, 0.22f, 0.55f);   // 음영진 바다
        Color landLight = new Color(0.55f, 0.78f, 0.35f);   // 밝은 대륙
        Color land = new Color(0.30f, 0.58f, 0.22f);        // 일반 대륙
        Color landDark = new Color(0.18f, 0.38f, 0.15f);    // 음영진 대륙
        Color ice = new Color(0.92f, 0.95f, 0.98f);         // 극지/구름
        Color desert = new Color(0.85f, 0.72f, 0.45f);      // 사막 (아프리카/중동)

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) continue;

                // 위도 (y) - 1.0=북극, -1.0=남극
                float lat = (y - cy) / radius;
                // 경도 (x) + 자전 오프셋
                float lon = (x - cx) / radius + longitudeOffset * 2f;

                // 음영: 왼쪽 위가 밝고 오른쪽 아래가 어두움 (지구 위쪽 빛)
                float shadingDX = dx / radius;
                float shadingDY = -dy / radius;
                float lightFactor = (shadingDX * -0.3f + shadingDY * 0.5f);
                lightFactor = Mathf.Clamp(lightFactor, -0.5f, 0.5f);

                // 극지 (위도 ±0.85 이상은 ice/구름)
                if (Mathf.Abs(lat) > 0.85f)
                {
                    pixels[y * size + x] = ice;
                    continue;
                }

                // 대륙 vs 바다 판정 - perlin noise 두 옥타브
                // 위도가 적도쪽일수록 대륙 적음 (해양 비율 ↑)
                float n1 = Mathf.PerlinNoise(lon * 1.5f + 100f, lat * 1.5f + 100f);
                float n2 = Mathf.PerlinNoise(lon * 4f + 50f, lat * 4f + 50f) * 0.5f;
                float continentNoise = n1 + n2;

                // 위도에 따라 land threshold 조정 (북반구에 land 살짝 더)
                float landThreshold = 0.85f - lat * 0.1f;
                bool isLand = continentNoise > landThreshold;

                Color baseColor;
                if (isLand)
                {
                    // 적도 근처 + 특정 경도면 사막
                    bool isDesert = Mathf.Abs(lat) < 0.35f && n2 > 0.3f && n1 < 1.0f;
                    if (isDesert)
                        baseColor = desert;
                    else if (continentNoise > 1.1f)
                        baseColor = landLight; // 산악/녹지
                    else
                        baseColor = land;
                }
                else
                {
                    // 해안가는 얕은 바다
                    if (continentNoise > 0.7f)
                        baseColor = oceanLight;
                    else
                        baseColor = ocean;
                }

                // 음영 적용
                Color shaded;
                if (lightFactor < -0.15f)
                {
                    // 그림자
                    shaded = isLand ? landDark : oceanDark;
                }
                else if (lightFactor > 0.2f && !isLand)
                {
                    // 바다 하이라이트
                    shaded = oceanLight;
                }
                else
                {
                    shaded = baseColor;
                }

                // 구름 (자전과 함께 약간 더 빠르게 움직임)
                float cloudNoise = Mathf.PerlinNoise(
                    (x + longitudeOffset * size * 1.3f) * 0.4f + 200f,
                    y * 0.4f + 200f);
                if (cloudNoise > 0.72f && Mathf.Abs(lat) < 0.85f)
                {
                    // 구름은 흰색이지만 그림자 영역에선 살짝 어둡게
                    shaded = lightFactor < -0.15f
                        ? Color.Lerp(shaded, new Color(0.6f, 0.6f, 0.65f), 0.7f)
                        : Color.Lerp(shaded, ice, 0.85f);
                }

                // 가장자리 어둡게 (구체감)
                float edgeFactor = dist / radius;
                if (edgeFactor > 0.85f)
                {
                    float edgeDarken = (edgeFactor - 0.85f) / 0.15f;
                    shaded = Color.Lerp(shaded, Color.black, edgeDarken * 0.4f);
                }

                pixels[y * size + x] = shaded;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ============ 기존 목적지 표시 ============

    void UpdateVisual()
    {
        // 인트로 출발 모드일 땐 일반 목적지 로직 건너뛰기
        // (OnStatsChanged 이벤트로 매 tick마다 호출되는데 그러면 지구가 꺼져버림)
        if (inDepartureMode) return;

        var gm = GameManager.Instance;
        if (gm == null || planetObj == null) return;

        if (gm.isDocked)
        {
            planetObj.SetActive(true);
            planetObj.transform.localPosition = new Vector3(arrivedX, yPosition, 0);
            planetObj.transform.localScale = Vector3.one * maxScale;
            if (glowObj != null)
            {
                glowObj.SetActive(true);
                glowObj.transform.localPosition = planetObj.transform.localPosition;
                glowObj.transform.localScale = Vector3.one * maxScale * 3f;
            }
            return;
        }

        if (gm.currentStarIndex != currentDestIndex)
        {
            currentDestIndex = gm.currentStarIndex;
            RegeneratePlanet(currentDestIndex);
        }

        StarData dest = StarDatabase.Stars[gm.currentStarIndex];
        float progress = dest.distanceKM > 0
            ? Mathf.Clamp01((float)(gm.distance / dest.distanceKM))
            : 0f;

        float zoom = zoomController != null ? zoomController.GetZoomNormalized() : 0f;

        float visibility = 0f;
        if (progress > 0.4f)
        {
            visibility = Mathf.InverseLerp(0.4f, 1f, progress);
        }

        if (visibility <= 0.01f)
        {
            planetObj.SetActive(false);
            if (glowObj != null) glowObj.SetActive(false);
            return;
        }

        planetObj.SetActive(true);
        float xPos = Mathf.Lerp(offScreenX, arrivedX, visibility);
        planetObj.transform.localPosition = new Vector3(xPos, yPosition, 0);
        float scale = Mathf.Lerp(minScale, maxScale, visibility);
        planetObj.transform.localScale = Vector3.one * scale;

        if (planetRenderer != null)
        {
            Color c = planetRenderer.color;
            c.a = Mathf.Lerp(0.3f, 1f, visibility);
            planetRenderer.color = c;
        }

        if (glowObj != null)
        {
            glowObj.SetActive(showGlow && visibility > 0.1f);
            glowObj.transform.localPosition = planetObj.transform.localPosition;
            glowObj.transform.localScale = Vector3.one * scale * 3f;
            if (glowRenderer != null)
            {
                StarData star = StarDatabase.Stars[gm.currentStarIndex];
                Color gc = star.color;
                gc.a = visibility * 0.2f;
                glowRenderer.color = gc;
            }
        }
    }

    void OnArrived(StarData star) { UpdateVisual(); }

    void RegeneratePlanet(int index)
    {
        if (planetTex != null) Destroy(planetTex);
        StarData star = StarDatabase.Stars[index];
        int size = 24;
        if (star.typeKey.Contains("giant") || star.typeKey.Contains("binary"))
            size = 32;
        else if (star.typeKey.Contains("dwarf") || star.typeKey.Contains("moon"))
            size = 16;

        planetTex = GenerateCelestialBody(size, star.color, index);
        if (planetRenderer != null)
            planetRenderer.sprite = Sprite.Create(planetTex, new Rect(0, 0, planetTex.width, planetTex.height), Vector2.one * 0.5f, 16);
    }

    Texture2D GenerateCelestialBody(int size, Color baseColor, int seed)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float radius = (size - 2) * 0.5f;
        float cx = size * 0.5f;
        float cy = size * 0.5f;

        Color light = new Color(baseColor.r * 1.3f, baseColor.g * 1.3f, baseColor.b * 1.3f);
        Color mid = baseColor;
        Color dark = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f);
        Color highlight = Color.Lerp(Color.white, baseColor, 0.4f);

        System.Random rng = new System.Random(seed);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) continue;

                float noise = (float)rng.NextDouble();
                Color c;
                if (dist > radius * 0.85f) c = dark;
                else if (dx < -radius * 0.25f && dy < -radius * 0.25f) c = noise > 0.5f ? highlight : light;
                else if (dx > radius * 0.25f && dy > radius * 0.25f) c = noise > 0.5f ? mid : dark;
                else c = noise > 0.6f ? light : noise > 0.35f ? mid : dark;
                pixels[y * size + x] = c;
            }
        tex.SetPixels(pixels); tex.Apply();
        return tex;
    }

    void CreatePlanetObject()
    {
        planetObj = new GameObject("DestinationPlanet");
        planetObj.transform.SetParent(transform);
        planetObj.transform.localPosition = new Vector3(offScreenX, yPosition, 0);
        planetRenderer = planetObj.AddComponent<SpriteRenderer>();
        planetRenderer.sortingOrder = 5;
        Texture2D tmp = new Texture2D(1, 1); tmp.SetPixel(0, 0, Color.white); tmp.Apply();
        planetRenderer.sprite = Sprite.Create(tmp, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        planetObj.SetActive(false);
    }

    void CreateGlowObject()
    {
        if (!showGlow) return;
        glowObj = new GameObject("DestinationGlow");
        glowObj.transform.SetParent(transform);
        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sortingOrder = 4;
        int gs = 32;
        glowTex = new Texture2D(gs, gs);
        glowTex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[gs * gs]; float gc = gs / 2f;
        for (int y = 0; y < gs; y++)
            for (int x = 0; x < gs; x++)
            { float d = Mathf.Sqrt((x - gc) * (x - gc) + (y - gc) * (y - gc)); float a = Mathf.Clamp01(1f - d / gc); a *= a; pixels[y * gs + x] = new Color(1, 1, 1, a * 0.5f); }
        glowTex.SetPixels(pixels); glowTex.Apply();
        glowRenderer.sprite = Sprite.Create(glowTex, new Rect(0, 0, gs, gs), Vector2.one * 0.5f, 16);
        glowObj.SetActive(false);
    }

    void OnDestroy()
    {
        if (planetTex != null) Destroy(planetTex);
        if (glowTex != null) Destroy(glowTex);
        if (departureTex != null) Destroy(departureTex);
        if (earthFrames != null)
        {
            for (int i = 0; i < earthFrames.Length; i++)
                if (earthFrames[i] != null) Destroy(earthFrames[i]);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged -= UpdateVisual;
            GameManager.Instance.OnStarArrived -= OnArrived;
        }
    }
}
