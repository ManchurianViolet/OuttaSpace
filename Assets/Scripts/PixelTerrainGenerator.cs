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

    private Texture2D terrainTex;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        gameObject.SetActive(false);
    }

    public void GenerateTerrain(StarData star)
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
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (terrainTex != null) Destroy(terrainTex);
    }
}