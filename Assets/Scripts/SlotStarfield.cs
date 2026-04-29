using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 영역 안에서 별들이 우→좌로 흘러감.
/// CatSlot에서 자동 생성됨. 마스크 없이 좌표 wrap만으로 영역 제한.
/// </summary>
public class SlotStarfield : MonoBehaviour
{
    [Header("Stars")]
    public int starCount = 12;
    public float minSpeed = 12f;     // UI px/s
    public float maxSpeed = 25f;

    private RectTransform rt;
    private RectTransform[] stars;
    private float[] speeds;

    private static Sprite cachedDot;

    void OnEnable()
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        // 슬롯 사이즈 강제 갱신 (Layout 아직 안 잡혔을 때 대비)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        if (stars == null) CreateStars();
        else Rescatter();
    }

    static void EnsureDotSprite()
    {
        if (cachedDot != null) return;
        Texture2D tex = new Texture2D(1, 1);
        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cachedDot = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void CreateStars()
    {
        EnsureDotSprite();

        stars = new RectTransform[starCount];
        speeds = new float[starCount];

        Vector2 size = rt.rect.size;
        if (size.x < 1 || size.y < 1) size = new Vector2(100, 60); // 폴백

        for (int i = 0; i < starCount; i++)
        {
            GameObject go = new GameObject($"Star_{i}");
            RectTransform sr = go.AddComponent<RectTransform>();  // 명시적으로 먼저
            sr.SetParent(transform, false);

            Image img = go.AddComponent<Image>();
            img.sprite = cachedDot;
            img.color = new Color(1f, 1f, 1f, Random.Range(0.6f, 1f));
            img.raycastTarget = false;

            sr.sizeDelta = new Vector2(2, 2);  // 1px → 2px
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = RandomPos(size);

            stars[i] = sr;
            speeds[i] = Random.Range(minSpeed, maxSpeed);
        }
    }
    Vector2 RandomPos(Vector2 size)
    {
        return new Vector2(
            Random.Range(-size.x * 0.5f, size.x * 0.5f),
            Random.Range(-size.y * 0.5f, size.y * 0.5f)
        );
    }

    void Rescatter()
    {
        Vector2 size = rt.rect.size;
        for (int i = 0; i < stars.Length; i++)
            if (stars[i] != null)
                stars[i].anchoredPosition = RandomPos(size);
    }

    void Update()
    {
        if (stars == null) return;
        Vector2 size = rt.rect.size;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;

            Vector2 p = stars[i].anchoredPosition;
            p.x -= speeds[i] * Time.deltaTime;

            if (p.x < -halfW)
            {
                p.x = halfW;
                p.y = Random.Range(-halfH, halfH);
                speeds[i] = Random.Range(minSpeed, maxSpeed);
            }
            stars[i].anchoredPosition = p;
        }
    }
}