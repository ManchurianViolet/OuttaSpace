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

        bool isSaturn = star.nameKey == "star_saturn";
        if (isSaturn)
        {
            planetTex = GenerateSaturn(size, star.color, index);
        }
        else if (star.typeKey.Contains("binary"))
        {
            planetTex = GenerateBinaryStar(size, star.color, star.typeKey, index);
        }
        else if (star.typeKey.Contains("gas_giant") || star.typeKey.Contains("ice_giant"))
        {
            planetTex = GenerateGasGiant(size, star.color, index);
        }
        else
        {
            planetTex = GenerateCelestialBody(size, star.color, index);
        }

        if (planetRenderer != null)
            planetRenderer.sprite = Sprite.Create(planetTex, new Rect(0, 0, planetTex.width, planetTex.height), Vector2.one * 0.5f, 16);
    }

    /// <summary>
    /// 쌍성: 두 개의 별이 가까이 붙어 있는 모양.
    /// type별 색 변형:
    ///  - white_binary (시리우스): 큰 별 흰색 + 작은 동반성
    ///  - red_binary (루이텐): 두 빨간 작은 별
    ///  - f_binary (프로키온): 큰 노랑흰 + 작은 흰
    ///  - k_binary (61백조성): 두 주황 별
    /// </summary>
    Texture2D GenerateBinaryStar(int size, Color baseColor, string typeKey, int seed)
    {
        // 가로로 두 별 그릴 공간 필요
        int texW = (int)(size * 1.5f);
        int texH = size;

        Texture2D tex = new Texture2D(texW, texH);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // 두 별의 크기/색 결정
        Color star1Color, star2Color;
        float star1Radius, star2Radius;

        if (typeKey.Contains("white"))
        {
            // 시리우스: 큰 흰 별 + 작은 동반성 (백색왜성)
            star1Color = new Color(0.95f, 0.97f, 1f); // 거의 흰색, 살짝 푸르스름
            star2Color = new Color(0.85f, 0.85f, 0.95f); // 동반성 더 차분한 흰
            star1Radius = size * 0.32f;
            star2Radius = size * 0.18f;
        }
        else if (typeKey.Contains("red"))
        {
            // 루이텐: 두 작은 빨간 별, 비슷한 크기
            star1Color = new Color(1f, 0.45f, 0.35f);
            star2Color = new Color(0.9f, 0.4f, 0.3f);
            star1Radius = size * 0.22f;
            star2Radius = size * 0.20f;
        }
        else if (typeKey.Contains("f_"))
        {
            // 프로키온: 큰 노랑흰 + 작은 흰 (백색왜성)
            star1Color = new Color(1f, 0.97f, 0.85f); // 노랑흰
            star2Color = new Color(0.9f, 0.9f, 0.95f); // 작은 흰
            star1Radius = size * 0.30f;
            star2Radius = size * 0.15f;
        }
        else // k_binary (61 백조성)
        {
            // 두 주황 별
            star1Color = new Color(1f, 0.78f, 0.5f);
            star2Color = new Color(0.95f, 0.7f, 0.45f);
            star1Radius = size * 0.25f;
            star2Radius = size * 0.22f;
        }

        // 두 별 위치 (살짝 겹치게)
        float cy = texH * 0.5f;
        float star1X = texW * 0.38f;
        float star2X = texW * 0.66f;

        // 별 1 그리기
        DrawStar(pixels, texW, texH, star1X, cy, star1Radius, star1Color, seed);
        // 별 2 그리기
        DrawStar(pixels, texW, texH, star2X, cy, star2Radius, star2Color, seed + 7);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 별 하나를 지정 위치에 그리기 (둥근 형태 + 음영 + 가장자리 어둡게).
    /// </summary>
    void DrawStar(Color[] pixels, int texW, int texH, float cx, float cy, float radius, Color baseColor, int seed)
    {
        Color light = new Color(
            Mathf.Min(1f, baseColor.r * 1.25f),
            Mathf.Min(1f, baseColor.g * 1.25f),
            Mathf.Min(1f, baseColor.b * 1.25f));
        Color mid = baseColor;
        Color dark = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f);
        Color highlight = Color.Lerp(Color.white, baseColor, 0.3f);

        System.Random rng = new System.Random(seed);

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 1));
        int maxX = Mathf.Min(texW - 1, Mathf.CeilToInt(cx + radius + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 1));
        int maxY = Mathf.Min(texH - 1, Mathf.CeilToInt(cy + radius + 1));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) continue;

                // 왼쪽 위 밝게, 오른쪽 아래 어둡게
                float lighting = (-dx / radius * 0.5f + dy / radius * 0.5f);
                Color c;
                if (dist > radius * 0.85f)
                {
                    c = dark; // 가장자리
                }
                else if (lighting > 0.3f)
                {
                    c = highlight; // 강한 하이라이트
                }
                else if (lighting > 0f)
                {
                    c = rng.NextDouble() > 0.4 ? light : mid;
                }
                else if (lighting > -0.3f)
                {
                    c = rng.NextDouble() > 0.5 ? mid : light;
                }
                else
                {
                    c = rng.NextDouble() > 0.5 ? mid : dark;
                }

                pixels[y * texW + x] = c;
            }
        }
    }

    /// <summary>
    /// 가스 거인 (목성/천왕성/해왕성). 가로 줄무늬 패턴.
    /// </summary>
    Texture2D GenerateGasGiant(int size, Color baseColor, int seed)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float radius = (size - 2) * 0.5f;
        float cx = size * 0.5f;
        float cy = size * 0.5f;

        Color light = new Color(baseColor.r * 1.25f, baseColor.g * 1.25f, baseColor.b * 1.25f);
        Color mid = baseColor;
        Color dark = new Color(baseColor.r * 0.55f, baseColor.g * 0.55f, baseColor.b * 0.55f);
        Color highlight = Color.Lerp(Color.white, baseColor, 0.5f);

        System.Random rng = new System.Random(seed);

        // 가로 띠 패턴 미리 결정 (각 y마다 어떤 톤인지)
        Color[] bandColors = new Color[size];
        for (int y = 0; y < size; y++)
        {
            // 위도에 따라 변화: 위/아래는 어둡게 (극지), 적도는 밝게
            float lat = (y - cy) / radius;
            float baseTone = 1f - Mathf.Abs(lat) * 0.4f;

            // 노이즈로 띠 두께 결정
            float n = Mathf.PerlinNoise(y * 0.3f + seed * 0.1f, seed * 0.2f);
            Color c;
            if (n > 0.65f) c = light;
            else if (n > 0.45f) c = highlight;
            else if (n > 0.25f) c = mid;
            else c = dark;

            // 톤 음영 적용
            c = new Color(c.r * baseTone, c.g * baseTone, c.b * baseTone);
            bandColors[y] = c;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) continue;

                Color c = bandColors[y];

                // 가로로 살짝 흔들림 (띠가 약간 굴절되는 효과)
                float wave = Mathf.PerlinNoise(x * 0.15f + seed * 0.3f, y * 0.2f);
                if (wave > 0.7f)
                {
                    // 인접 띠와 약간 섞임
                    int neighborY = (y + (wave > 0.85f ? 1 : -1) + size) % size;
                    c = Color.Lerp(c, bandColors[neighborY], 0.5f);
                }

                // 오른쪽 위 음영 → 입체감
                float shading = (-dx / radius * 0.3f + dy / radius * 0.3f);
                if (shading > 0.15f)
                    c = Color.Lerp(c, Color.black, shading * 0.4f);

                // 가장자리 어둡게
                if (dist > radius * 0.85f)
                {
                    float edgeDarken = (dist / radius - 0.85f) / 0.15f;
                    c = Color.Lerp(c, Color.black, edgeDarken * 0.5f);
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 토성: 가스 거인 본체 + 가로 고리.
    /// 행성보다 가로로 넓은 텍스처에 그림.
    /// </summary>
    Texture2D GenerateSaturn(int planetSize, Color baseColor, int seed)
    {
        // 고리가 행성 양옆으로 뻗어나오는 크기
        int texW = (int)(planetSize * 2.2f);
        int texH = planetSize;

        Texture2D tex = new Texture2D(texW, texH);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float planetRadius = (planetSize - 2) * 0.5f;
        float cx = texW * 0.5f;
        float cy = texH * 0.5f;

        // 행성 본체 색 (가스 띠)
        Color light = new Color(baseColor.r * 1.25f, baseColor.g * 1.25f, baseColor.b * 1.25f);
        Color mid = baseColor;
        Color dark = new Color(baseColor.r * 0.55f, baseColor.g * 0.55f, baseColor.b * 0.55f);
        Color highlight = Color.Lerp(Color.white, baseColor, 0.5f);

        // 고리 색 (행성보다 밝고 채도 낮음)
        Color ringLight = Color.Lerp(baseColor, Color.white, 0.6f);
        Color ringMid = Color.Lerp(baseColor, new Color(0.9f, 0.85f, 0.7f), 0.5f);
        Color ringDark = Color.Lerp(baseColor, new Color(0.4f, 0.35f, 0.3f), 0.5f);

        // 행성 띠
        Color[] bandColors = new Color[texH];
        for (int y = 0; y < texH; y++)
        {
            float lat = (y - cy) / planetRadius;
            float baseTone = 1f - Mathf.Abs(lat) * 0.4f;

            float n = Mathf.PerlinNoise(y * 0.3f + seed * 0.1f, seed * 0.2f);
            Color c;
            if (n > 0.65f) c = light;
            else if (n > 0.45f) c = highlight;
            else if (n > 0.25f) c = mid;
            else c = dark;

            c = new Color(c.r * baseTone, c.g * baseTone, c.b * baseTone);
            bandColors[y] = c;
        }

        // 고리 파라미터: 행성보다 살짝 위로 기울어진 가로 타원
        // 고리의 가로 반경 = planetRadius * 2.0, 세로 반경 = planetRadius * 0.35
        float ringRadiusX = planetRadius * 2.0f;
        float ringRadiusYOuter = planetRadius * 0.35f;
        float ringRadiusYInner = planetRadius * 0.20f;
        float ringTilt = -0.15f; // 살짝 위로 기울어짐 (음수면 왼쪽이 올라감)

        // 1단계: 행성 뒤쪽 고리 (행성 영역보다 위쪽 절반)
        DrawRing(pixels, texW, texH, cx, cy, ringRadiusX, ringRadiusYOuter, ringRadiusYInner,
                 ringTilt, planetRadius, ringLight, ringMid, ringDark, isBack: true);

        // 2단계: 행성 본체
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > planetRadius) continue;

                Color c = bandColors[y];

                float wave = Mathf.PerlinNoise(x * 0.15f + seed * 0.3f, y * 0.2f);
                if (wave > 0.7f)
                {
                    int neighborY = (y + (wave > 0.85f ? 1 : -1) + texH) % texH;
                    bandColors[neighborY] = Color.Lerp(bandColors[neighborY], c, 0.5f);
                    c = Color.Lerp(c, bandColors[neighborY], 0.5f);
                }

                float shading = (-dx / planetRadius * 0.3f + dy / planetRadius * 0.3f);
                if (shading > 0.15f)
                    c = Color.Lerp(c, Color.black, shading * 0.4f);

                if (dist > planetRadius * 0.85f)
                {
                    float edgeDarken = (dist / planetRadius - 0.85f) / 0.15f;
                    c = Color.Lerp(c, Color.black, edgeDarken * 0.5f);
                }

                pixels[y * texW + x] = c;
            }
        }

        // 3단계: 행성 앞쪽 고리 (행성 영역보다 아래쪽 절반)
        DrawRing(pixels, texW, texH, cx, cy, ringRadiusX, ringRadiusYOuter, ringRadiusYInner,
                 ringTilt, planetRadius, ringLight, ringMid, ringDark, isBack: false);

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 토성 고리 한 부분 그리기 (뒤쪽 절반 또는 앞쪽 절반).
    /// isBack=true: 행성 위쪽 (행성 뒤로 가는 부분)
    /// isBack=false: 행성 아래쪽 (행성 앞으로 오는 부분)
    /// </summary>
    void DrawRing(Color[] pixels, int texW, int texH, float cx, float cy,
        float rX, float rYOuter, float rYInner, float tilt, float planetR,
        Color ringLight, Color ringMid, Color ringDark, bool isBack)
    {
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float dx = x - cx;
                float dy = y - cy;

                // 기울임 적용 (회전된 좌표)
                float rotDy = dy - dx * tilt;

                // 타원 외/내곽 판정
                float outerDist = (dx * dx) / (rX * rX) + (rotDy * rotDy) / (rYOuter * rYOuter);
                float innerDist = (dx * dx) / (rX * rX) + (rotDy * rotDy) / (rYInner * rYInner);

                // 고리 영역: outer ≤ 1 AND inner ≥ 1
                if (outerDist > 1f || innerDist < 1f) continue;

                // 행성 본체랑 겹치는 영역은 isBack 분기로 처리
                float planetDist = Mathf.Sqrt(dx * dx + dy * dy);
                bool overlapsPlanet = planetDist <= planetR;

                if (isBack)
                {
                    // 뒤쪽 고리: 행성 위쪽만 (rotDy < 0)
                    if (rotDy > 0) continue;
                    if (overlapsPlanet) continue;
                }
                else
                {
                    // 앞쪽 고리: 행성 아래쪽만 (rotDy > 0). 행성 위에 덮어쓰기 가능
                    if (rotDy < 0) continue;
                }

                // 고리 안에서 어디쯤인지 (안쪽=진하고, 바깥쪽=밝게)
                float ringT = (outerDist - innerDist * 0.5f) / 0.5f;
                Color c;
                if (ringT > 0.7f) c = ringLight;
                else if (ringT > 0.3f) c = ringMid;
                else c = ringDark;

                // 약간의 줄무늬 (Cassini division 같은 어두운 갭)
                float ringNoise = Mathf.PerlinNoise(outerDist * 8f, 0f);
                if (ringNoise > 0.7f)
                    c = Color.Lerp(c, Color.black, 0.4f);

                pixels[y * texW + x] = c;
            }
        }
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