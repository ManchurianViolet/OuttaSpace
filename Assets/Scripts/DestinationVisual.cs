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

    private GameObject planetObj;
    private SpriteRenderer planetRenderer;
    private GameObject glowObj;
    private SpriteRenderer glowRenderer;
    private Texture2D planetTex;
    private Texture2D glowTex;
    private int currentDestIndex = -1;

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
        UpdateVisual();
        if (planetObj != null)
        {
            float wobble = Mathf.Sin(Time.time * 0.3f) * 0.05f;
            var pos = planetObj.transform.localPosition;
            planetObj.transform.localPosition = new Vector3(pos.x, yPosition + wobble, 0);
        }
    }

    void UpdateVisual()
    {
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

        // 40%부터 무조건 보임
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

        float center = size / 2f; float radius = size / 2f - 1;
        Color dark = baseColor * 0.4f; dark.a = 1;
        Color mid = baseColor * 0.7f; mid.a = 1;
        Color light = baseColor; light.a = 1;
        Color highlight = Color.Lerp(baseColor, Color.white, 0.4f); highlight.a = 1;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center, dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius) continue;
                float noise = Mathf.PerlinNoise((x + seed * 13.7f) * 0.2f, (y + seed * 7.3f) * 0.2f);
                Color c;
                if (dist > radius - 1.2f) c = dark;
                else if (dx < -radius * 0.2f && dy < -radius * 0.2f && dist < radius * 0.5f) c = noise > 0.5f ? highlight : light;
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsChanged -= UpdateVisual;
            GameManager.Instance.OnStarArrived -= OnArrived;
        }
    }
}