using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 친구창의 한 행. 좌측 이름/거리/상태, 가운데 고양이, 우측 활동.
///
/// 활동 표시 분기:
/// - Sailing (실시간 packet 있음): "X로 가는 중 (Y%)" 또는 "X에서 휴식 중"
/// - Offline + 정박 중 종료 (wasDocked=true): "X에서 휴식 중"
/// - Offline + 항해 중 종료 (wasDocked=false): "X로 가는 길 어딘가"
///
/// 항해 중(실시간 Sailing + !isDocked)인 친구 카드는:
/// - 배경에 별이 왼쪽으로 흐르는 우주 스크롤 효과
/// - 고양이 이미지가 살짝 호버링 (위아래 부유)
/// 친구마다 호버 phase/속도가 다르게 deterministic seed로 결정됨.
/// </summary>
public class FriendCardUI : MonoBehaviour
{
    [Header("Left Column")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI distanceText;
    public Image statusDot;
    public TextMeshProUGUI statusText;

    [Header("Center")]
    public Image catImage;

    [Header("Right Column")]
    public TextMeshProUGUI activityText;

    [Header("Status Colors")]
    public Color sailingColor = new Color(0.36f, 0.79f, 0.36f);  // 초록
    public Color onlineColor = new Color(1f, 0.84f, 0f);         // 노랑
    public Color offlineColor = new Color(0.5f, 0.5f, 0.5f);     // 회색

    [Header("Star Scroll (항해 중에만)")]
    public int starCount = 20;
    public Vector2 starSpeedRange = new Vector2(20f, 60f);
    public Vector2 starSizeRange = new Vector2(1.5f, 3f);
    public Vector2 starAlphaRange = new Vector2(0.3f, 0.8f);

    [Header("Cat Hover (항해 중에만)")]
    [Tooltip("고양이가 위아래로 움직이는 진폭 (픽셀).")]
    public float hoverAmplitude = 4f;
    [Tooltip("호버링 속도 (사이클/초). 1 = 초당 한 번 위아래 왕복.")]
    public Vector2 hoverSpeedRange = new Vector2(0.8f, 1.4f);

    private RectTransform starContainer;
    private List<StarPixel> stars = new List<StarPixel>();
    private bool sailingActive;
    private static Sprite sharedStarSprite;

    // 호버
    private Vector2 catBaseAnchoredPos;
    private bool catBasePosCached;
    private float hoverPhase;
    private float hoverSpeed;

    class StarPixel
    {
        public RectTransform rect;
        public Image image;
        public float speed;
        public float baseAlpha;
    }

    public void Bind(FriendEntry e)
    {
        // 이름
        if (nameText != null)
            nameText.text = e.isMe ? "You" : e.displayName;

        // 누적거리
        if (distanceText != null)
        {
            string label = Loc.Get("far_from_earth");
            distanceText.text = $"{label}\n{StarDatabase.FormatKM(e.cumulativeKM)}";
        }

        // 상태
        if (e.isMe)
        {
            if (statusDot != null) statusDot.enabled = false;
            if (statusText != null) statusText.text = "";
        }
        else
        {
            if (statusDot != null) statusDot.enabled = true;
            ApplyStatus(e.status);
        }

        // 고양이 스킨
        if (catImage != null && CatDatabase.Instance != null)
        {
            int safeId = Mathf.Clamp(e.catId, 0, CatDatabase.TOTAL_COUNT - 1);
            CatData cat = CatDatabase.Instance.Get(safeId);
            if (cat != null && cat.sprite != null)
            {
                catImage.sprite = cat.sprite;
                catImage.enabled = true;
                catImage.preserveAspect = true;
            }
            else
            {
                catImage.enabled = false;
            }
        }

        // 활동 표시 + sailing 여부 결정
        bool isSailing = false;
        if (activityText != null)
        {
            int safeStarIdx = Mathf.Max(e.currentStarIndex, 0); // 무한 모드: 상한 없음
            StarData star = StarDatabase.GetStar(safeStarIdx);
            string starName = Loc.Get(star.nameKey);

            if (e.status == FriendStatus.Sailing && !e.isDocked)
            {
                isSailing = true;
                float pct = (star.distanceKM > 0)
                    ? Mathf.Clamp01((float)(e.distanceKM / star.distanceKM)) * 100f
                    : 0f;
                activityText.text = Loc.Get("friend_heading_to", starName, $"{pct:F0}");
            }
            else if (e.isDocked)
            {
                activityText.text = Loc.Get("friend_resting_on", starName);
            }
            else
            {
                activityText.text = Loc.Get("friend_drifting_to", starName);
            }
        }

        // 친구별 고유 호버 파라미터 결정 (같은 친구는 항상 같은 리듬)
        // 본인(isMe)은 steamId=0이라 0 seed, 다른 친구들은 각자 seed
        System.Random rng = new System.Random(e.isMe ? 0 : e.steamId.GetHashCode());
        hoverPhase = (float)(rng.NextDouble() * Mathf.PI * 2f);
        hoverSpeed = Mathf.Lerp(hoverSpeedRange.x, hoverSpeedRange.y, (float)rng.NextDouble());

        SetSailing(isSailing);
    }

    void ApplyStatus(FriendStatus s)
    {
        Color c;
        string key;
        switch (s)
        {
            case FriendStatus.Sailing: c = sailingColor; key = "status_sailing"; break;
            case FriendStatus.Online: c = onlineColor; key = "status_online"; break;
            default: c = offlineColor; key = "status_offline"; break;
        }

        if (statusDot != null) statusDot.color = c;
        if (statusText != null)
        {
            statusText.text = Loc.Get(key);
            statusText.color = c;
        }
    }

    // ============ 별 스크롤 ============

    void SetSailing(bool sailing)
    {
        if (sailing)
        {
            EnsureStars();
            if (starContainer != null) starContainer.gameObject.SetActive(true);
        }
        else
        {
            if (starContainer != null) starContainer.gameObject.SetActive(false);
            // 호버 멈춤 → 고양이 원위치 복귀
            if (catBasePosCached && catImage != null)
                catImage.rectTransform.anchoredPosition = catBaseAnchoredPos;
        }
        sailingActive = sailing;
    }

    void EnsureStars()
    {
        if (starContainer != null) return;

        if (sharedStarSprite == null)
            sharedStarSprite = BuildStarSprite();

        GameObject containerObj = new GameObject("StarScroll", typeof(RectTransform), typeof(RectMask2D));
        containerObj.transform.SetParent(transform, worldPositionStays: false);
        containerObj.transform.SetSiblingIndex(0);

        starContainer = containerObj.GetComponent<RectTransform>();
        starContainer.anchorMin = Vector2.zero;
        starContainer.anchorMax = Vector2.one;
        starContainer.offsetMin = Vector2.zero;
        starContainer.offsetMax = Vector2.zero;
        starContainer.pivot = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObj = new GameObject("Star" + i, typeof(RectTransform), typeof(Image));
            starObj.transform.SetParent(starContainer, worldPositionStays: false);

            RectTransform rt = starObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(starSizeRange.x, starSizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            Image img = starObj.GetComponent<Image>();
            img.sprite = sharedStarSprite;
            img.raycastTarget = false;

            float sizeT = Mathf.InverseLerp(starSizeRange.x, starSizeRange.y, size);
            float alpha = Mathf.Lerp(starAlphaRange.x, starAlphaRange.y, sizeT);
            float speed = Mathf.Lerp(starSpeedRange.x, starSpeedRange.y, sizeT);

            StarPixel sp = new StarPixel
            {
                rect = rt,
                image = img,
                speed = speed,
                baseAlpha = alpha,
            };
            stars.Add(sp);
            sp.image.color = new Color(1f, 1f, 1f, alpha);
        }

        SpreadInitial();
    }

    void SpreadInitial()
    {
        if (starContainer == null) return;

        float width = starContainer.rect.width;
        float height = starContainer.rect.height;
        if (width <= 0) width = 400;
        if (height <= 0) height = 60;

        foreach (var sp in stars)
        {
            float x = Random.Range(0f, width);
            float y = Random.Range(-height * 0.5f, height * 0.5f);
            sp.rect.anchoredPosition = new Vector2(x, y);
        }
    }

    void Update()
    {
        if (!sailingActive) return;

        // 별 스크롤
        if (starContainer != null)
        {
            float width = starContainer.rect.width;
            float height = starContainer.rect.height;
            if (width > 0 && height > 0)
            {
                float dt = Time.deltaTime;
                foreach (var sp in stars)
                {
                    Vector2 pos = sp.rect.anchoredPosition;
                    pos.x -= sp.speed * dt;
                    if (pos.x < -5f)
                    {
                        pos.x = width + Random.Range(0f, 20f);
                        pos.y = Random.Range(-height * 0.5f, height * 0.5f);
                    }
                    sp.rect.anchoredPosition = pos;
                }
            }
        }

        // 고양이 호버링
        if (catImage != null)
        {
            // base 위치를 첫 호버 적용 전에 캐싱 (Bind에서는 RectTransform이 아직 layout 안 끝날 수 있음)
            if (!catBasePosCached)
            {
                catBaseAnchoredPos = catImage.rectTransform.anchoredPosition;
                catBasePosCached = true;
            }

            float bob = Mathf.Sin(Time.time * hoverSpeed * Mathf.PI * 2f + hoverPhase) * hoverAmplitude;
            Vector2 pos = catBaseAnchoredPos;
            pos.y += bob;
            catImage.rectTransform.anchoredPosition = pos;
        }
    }

    /// <summary>
    /// 1x1 흰색 픽셀 sprite. 별 색은 모두 흰색이고 알파만 다름.
    /// </summary>
    static Sprite BuildStarSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}