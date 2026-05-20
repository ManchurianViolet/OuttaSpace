using UnityEngine;

public class PixelTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int width = 256;
    public int height = 80;
    public int pixelsPerUnit = 16;
    public int sortingOrder = 8;

    [Header("Position")]
    public float yOffset = -3.5f;

    [Header("Earth Intro Scroll")]
    [Tooltip("지구 표면을 왼쪽으로 흘려 우주선이 움직이는 느낌을 줌. 0이면 정지.")]
    public float earthScrollSpeed = 0f;
    [Tooltip("지구 인트로 표면 너비 (2배로 만들어 loop). 일반 행성보다 넓음.")]
    public int earthWidth = 512;

    private Texture2D terrainTex;
    private SpriteRenderer sr;

    private bool isEarthMode;
    private float earthScrollOffset;
    private Vector3 earthBasePosition;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isEarthMode || earthScrollSpeed <= 0) return;

        // 지구 표면을 왼쪽으로 이동 → 우주선이 오른쪽으로 가는 느낌
        earthScrollOffset += earthScrollSpeed * Time.deltaTime;

        // earthWidth의 절반만큼 이동했으면 reset (loop)
        // 표면이 실제 2배 너비라 절반 지점에서 wrap하면 끊김 없이 보임
        float halfWidthWorld = (earthWidth * 0.5f) / pixelsPerUnit;
        if (earthScrollOffset >= halfWidthWorld)
            earthScrollOffset -= halfWidthWorld;

        transform.localPosition = earthBasePosition + new Vector3(-earthScrollOffset, 0, 0);
    }

    public void GenerateTerrain(StarData star)
    {
        // 일반 행성 모드면 earth 모드 종료
        if (star == null || star.nameKey != "star_earth")
        {
            isEarthMode = false;
            GenerateNormalTerrain(star);
            return;
        }

        // 지구 인트로 전용
        GenerateEarthTerrain();
    }

    void GenerateNormalTerrain(StarData star)
    {
        if (terrainTex != null) Destroy(terrainTex);

        terrainTex = new Texture2D(width, height);
        terrainTex.filterMode = FilterMode.Point;
        terrainTex.wrapMode = TextureWrapMode.Clamp;

        Color baseColor = star.color;
        Color[] palette = GeneratePalette(baseColor, star.typeKey);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int seed = star.nameKey.GetHashCode();
        System.Random rng = new System.Random(seed);

        float[] heightMap = new float[width];
        float baseHeight = height * 0.7f;

        for (int x = 0; x < width; x++)
        {
            float n1 = Mathf.PerlinNoise((x + seed) * 0.02f, seed * 0.1f) * 12f;
            float n2 = Mathf.PerlinNoise((x + seed) * 0.06f, seed * 0.2f) * 5f;
            float n3 = Mathf.PerlinNoise((x + seed) * 0.12f, seed * 0.3f) * 2f;
            heightMap[x] = baseHeight + n1 + n2 + n3;
        }

        for (int x = 0; x < width; x++)
        {
            int surfaceY = Mathf.Clamp((int)heightMap[x], 4, height - 1);

            for (int y = 0; y <= surfaceY; y++)
            {
                int depth = surfaceY - y;
                Color c;

                if (y == surfaceY)
                    c = palette[3];
                else if (depth <= 1)
                {
                    float d = Mathf.PerlinNoise((x + seed) * 0.2f, (y + seed) * 0.3f);
                    c = d > 0.5f ? palette[3] : palette[2];
                }
                else if (depth <= 4)
                {
                    float d = Mathf.PerlinNoise((x + seed) * 0.1f, (y + seed) * 0.15f);
                    c = d > 0.55f ? palette[2] : palette[1];
                }
                else
                {
                    float d = Mathf.PerlinNoise((x + seed) * 0.07f, (y + seed) * 0.08f);
                    c = d > 0.6f ? palette[1] : palette[0];
                }

                pixels[y * width + x] = c;
            }

            if (rng.NextDouble() < 0.02f)
            {
                int rockH = rng.Next(2, 5);
                for (int ry = 1; ry <= rockH; ry++)
                {
                    int rockW = rockH - ry + 1;
                    for (int rx = -rockW; rx <= rockW; rx++)
                    {
                        int px = x + rx;
                        int py = surfaceY + ry;
                        if (px >= 0 && px < width && py < height)
                            pixels[py * width + px] = ry >= rockH ? palette[3] : palette[2];
                    }
                }
            }

            if (rng.NextDouble() < 0.06f)
            {
                int py = surfaceY + 1;
                if (py < height)
                    pixels[py * width + x] = palette[2];
            }
        }

        terrainTex.SetPixels(pixels);
        terrainTex.Apply();

        sr.sprite = Sprite.Create(
            terrainTex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 1f),
            pixelsPerUnit
        );

        transform.localPosition = new Vector3(0, yOffset, 0);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 지구 지표면 전용. 풀밭 + 흙 + 돌 + 나무 + 산.
    /// 너비를 earthWidth (보통 화면 2배)로 만들어 가로 스크롤 loop 가능.
    /// </summary>
    void GenerateEarthTerrain()
    {
        if (terrainTex != null) Destroy(terrainTex);

        int w = earthWidth; // 지구는 일반 너비보다 넓게
        terrainTex = new Texture2D(w, height);
        terrainTex.filterMode = FilterMode.Point;
        terrainTex.wrapMode = TextureWrapMode.Clamp;

        // 지구 팔레트
        Color grassLight = new Color(0.45f, 0.75f, 0.32f);
        Color grass      = new Color(0.30f, 0.58f, 0.22f);
        Color grassDark  = new Color(0.18f, 0.40f, 0.14f);
        Color dirt       = new Color(0.45f, 0.30f, 0.18f);
        Color dirtDark   = new Color(0.28f, 0.18f, 0.10f);
        Color rockGray   = new Color(0.42f, 0.40f, 0.40f);
        Color stone      = new Color(0.22f, 0.20f, 0.20f);
        Color treeTrunk  = new Color(0.32f, 0.20f, 0.10f);
        Color treeLeaf   = new Color(0.20f, 0.50f, 0.18f);
        Color treeLeafL  = new Color(0.35f, 0.65f, 0.25f);
        Color mountain   = new Color(0.40f, 0.38f, 0.42f);
        Color mountainD  = new Color(0.25f, 0.23f, 0.27f);
        Color mountainSnow = new Color(0.90f, 0.92f, 0.95f);

        Color[] pixels = new Color[w * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int seed = "star_earth".GetHashCode();
        System.Random rng = new System.Random(seed);

        // 풀밭 높이 맵 — 절반 너비만 계산하고 나머지 절반은 복사 (loop 가능)
        int halfW = w / 2;
        float[] heightMap = new float[halfW];
        float baseHeight = height * 0.55f;
        for (int x = 0; x < halfW; x++)
        {
            float n1 = Mathf.PerlinNoise((x + seed) * 0.025f, seed * 0.1f) * 6f;
            float n2 = Mathf.PerlinNoise((x + seed) * 0.08f, seed * 0.2f) * 2.5f;
            heightMap[x] = baseHeight + n1 + n2;
        }

        // 1단계: 풀밭 + 흙 + 돌 (절반만 칠하고 나중에 복사)
        for (int x = 0; x < halfW; x++)
        {
            int surfaceY = Mathf.Clamp((int)heightMap[x], 4, height - 1);

            for (int y = 0; y <= surfaceY; y++)
            {
                int depth = surfaceY - y;
                Color c;

                if (y == surfaceY)
                {
                    float n = Mathf.PerlinNoise((x + seed) * 0.3f, seed * 0.4f);
                    c = n > 0.55f ? grassLight : grass;
                }
                else if (depth == 1)
                {
                    float n = Mathf.PerlinNoise((x + seed) * 0.25f, (y + seed) * 0.3f);
                    c = n > 0.6f ? grassDark : dirt;
                }
                else if (depth <= 5)
                {
                    float n = Mathf.PerlinNoise((x + seed) * 0.1f, (y + seed) * 0.15f);
                    c = n > 0.55f ? dirt : dirtDark;
                }
                else if (depth <= 12)
                {
                    float n = Mathf.PerlinNoise((x + seed) * 0.08f, (y + seed) * 0.1f);
                    c = n > 0.6f ? rockGray : dirtDark;
                }
                else
                {
                    float n = Mathf.PerlinNoise((x + seed) * 0.06f, (y + seed) * 0.08f);
                    c = n > 0.5f ? rockGray : stone;
                }

                pixels[y * w + x] = c;
            }

            if (rng.NextDouble() < 0.12f)
            {
                int py = surfaceY + 1;
                if (py < height)
                    pixels[py * w + x] = grassDark;
            }

            if (rng.NextDouble() < 0.015f)
            {
                int py = surfaceY + 1;
                if (py < height)
                {
                    Color flower = rng.NextDouble() < 0.5
                        ? new Color(1f, 0.9f, 0.3f)
                        : new Color(1f, 0.7f, 0.85f);
                    pixels[py * w + x] = flower;
                }
            }
        }

        // 2단계: 나무 배치
        for (int x = 2; x < halfW - 2; x++)
        {
            if (rng.NextDouble() < 0.08f)
            {
                int surfaceY = Mathf.Clamp((int)heightMap[x], 4, height - 1);
                int trunkHeight = rng.Next(3, 6);
                int leafRadius = rng.Next(2, 4);

                for (int ty = 1; ty <= trunkHeight; ty++)
                {
                    int py = surfaceY + ty;
                    if (py < height)
                    {
                        pixels[py * w + x] = treeTrunk;
                        if (ty <= trunkHeight - 1 && x + 1 < halfW && rng.NextDouble() < 0.4)
                            pixels[py * w + (x + 1)] = treeTrunk;
                    }
                }

                int leafCenterY = surfaceY + trunkHeight + leafRadius - 1;
                for (int ly = -leafRadius; ly <= leafRadius; ly++)
                {
                    for (int lx = -leafRadius; lx <= leafRadius; lx++)
                    {
                        float ld = Mathf.Sqrt(lx * lx + ly * ly);
                        if (ld > leafRadius + 0.3f) continue;
                        int px = x + lx;
                        int py = leafCenterY + ly;
                        if (px < 0 || px >= halfW || py < 0 || py >= height) continue;

                        if (ld > leafRadius - 0.5f && rng.NextDouble() < 0.4) continue;

                        Color leafColor = (ly > 0 || rng.NextDouble() < 0.5) ? treeLeafL : treeLeaf;
                        pixels[py * w + px] = leafColor;
                    }
                }

                x += rng.Next(8, 16);
            }
        }

        // 3단계: 산 (절반 영역에 2-4개)
        int mountainCount = rng.Next(2, 5);
        for (int m = 0; m < mountainCount; m++)
        {
            int mx = rng.Next(halfW / 4, halfW * 3 / 4) + rng.Next(-20, 20);
            int mHeight = rng.Next(15, 25);
            int mWidth = mHeight * 2;
            int mBaseY = (int)(baseHeight - 4);

            for (int my = 0; my <= mHeight; my++)
            {
                int currentWidth = mWidth * (mHeight - my) / mHeight;
                for (int mxOff = -currentWidth / 2; mxOff <= currentWidth / 2; mxOff++)
                {
                    int px = mx + mxOff;
                    int py = mBaseY + my;
                    if (px < 0 || px >= halfW || py < 0 || py >= height) continue;

                    if (pixels[py * w + px].a > 0.01f) continue;

                    Color mc;
                    float shading = mxOff / (float)(currentWidth * 0.5f);
                    if (my >= mHeight - 3 && currentWidth < mWidth / 4)
                        mc = mountainSnow;
                    else if (shading < -0.2f)
                        mc = mountainD;
                    else
                        mc = mountain;

                    pixels[py * w + px] = mc;
                }
            }
        }

        // 4단계: 절반 영역을 오른쪽 절반으로 복사 (loop용)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < halfW; x++)
            {
                pixels[y * w + (x + halfW)] = pixels[y * w + x];
            }
        }

        terrainTex.SetPixels(pixels);
        terrainTex.Apply();

        sr.sprite = Sprite.Create(
            terrainTex,
            new Rect(0, 0, w, height),
            new Vector2(0.5f, 1f),
            pixelsPerUnit
        );

        // 지구 모드 활성 + 스크롤 시작 위치 저장
        isEarthMode = true;
        earthScrollOffset = 0f;
        earthBasePosition = new Vector3(0, yOffset, 0);
        transform.localPosition = earthBasePosition;

        gameObject.SetActive(true);
    }

    Color[] GeneratePalette(Color baseColor, string typeKey)
    {
        Color ground;

        if (typeKey.Contains("moon"))
            ground = new Color(0.55f, 0.53f, 0.5f);
        else if (typeKey.Contains("ice"))
            ground = Color.Lerp(baseColor, new Color(0.7f, 0.8f, 0.9f), 0.5f);
        else if (typeKey.Contains("gas"))
            ground = Color.Lerp(baseColor, new Color(0.45f, 0.35f, 0.25f), 0.5f);
        else if (typeKey.Contains("terrestrial"))
            ground = Color.Lerp(baseColor, new Color(0.5f, 0.4f, 0.3f), 0.4f);
        else
            ground = baseColor * 0.35f;

        ground.a = 1f;

        return new Color[]
        {
            DarkBlend(ground, 0.2f),
            DarkBlend(ground, 0.45f),
            DarkBlend(ground, 0.7f),
            ground,
        };
    }

    Color DarkBlend(Color c, float t)
    {
        Color dark = Color.Lerp(Color.black, c, t);
        dark.a = 1f;
        return dark;
    }

    public void Hide()
    {
        isEarthMode = false;
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (terrainTex != null) Destroy(terrainTex);
    }
}
