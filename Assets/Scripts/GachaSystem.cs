using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GachaSystem : MonoBehaviour
{
    public static GachaSystem Instance { get; private set; }

    [Header("Panel")]
    public GameObject gachaPanel;

    [Header("Slot Reel")]
    public RectTransform slotMask;
    public Image[] slotSlots;
    public float slotHeight = 160f;

    [Header("UI")]
    public TextMeshProUGUI resultNameText;
    public Button drawButton;
    public TextMeshProUGUI drawButtonText;
    public Button closeButton;

    [Header("Result FX")]
    [Tooltip("전설 플래시용 풀스크린 Image (가챠 패널 최상단, 흰색, 알파 0, Raycast Target 끄기). 비우면 플래시 생략.")]
    public Image flashOverlay;
    [Tooltip("NEW! / +30 CR 뱃지 TMP. 비우면 뱃지 생략.")]
    public TextMeshProUGUI resultBadgeText;

    [Header("Animation")]
    public float totalDuration = 5f;
    public int totalCycles = 25;

    [Header("Button Visual")]
    public Color buttonNormalColor = Color.white;
    public Color buttonRollingColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private bool isRolling;
    private Color originalTextColor = Color.white;
    private bool textColorCached;

    // 연출 상태
    private readonly List<Coroutine> fxCoroutines = new List<Coroutine>();
    private readonly List<GameObject> activeShards = new List<GameObject>();
    private Vector2 badgeOriginalPos;
    private bool badgePosCached;
    private Vector2 slotMaskOriginalPos;
    private bool slotMaskPosCached;
    private static Sprite shardSprite; // 4x4 픽셀 파편 (절차 생성, 공유)

    private static readonly Color GOLD = new Color(1f, 0.84f, 0f);        // #ffd700
    private static readonly Color SHARD_BLUE = new Color(0.4f, 0.65f, 1f);
    private static readonly Color REFUND_GREEN = new Color(0.4f, 0.87f, 0.4f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (drawButton != null)
            drawButton.onClick.AddListener(OnDrawClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        UpdateButtonText();
        if (resultNameText != null)
            resultNameText.text = "";
        if (resultBadgeText != null)
            resultBadgeText.text = "";
        if (flashOverlay != null)
        {
            var c = flashOverlay.color; c.a = 0f; flashOverlay.color = c;
            flashOverlay.raycastTarget = false;
        }
    }

    void OnEnable() => UpdateButtonText();

    void Update()
    {
        if (!isRolling)
            UpdateButtonText();
    }

    void UpdateButtonText()
    {
        if (drawButtonText != null)
            drawButtonText.text = Loc.Get("gacha_pull_cost", CatDatabase.GACHA_COST);

        if (drawButton != null && !isRolling)
        {
            bool canAfford = GameManager.Instance != null
                && GameManager.Instance.credits >= CatDatabase.GACHA_COST;
            drawButton.interactable = canAfford;
        }
    }

    public void OpenPanel()
    {
        if (gachaPanel != null) gachaPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (isRolling) return;
        if (gachaPanel != null) gachaPanel.SetActive(false);
    }

    void OnDrawClicked() => TryGacha();

    public bool TryGacha()
    {
        if (isRolling) return false;
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.credits < CatDatabase.GACHA_COST) return false;

        GameManager.Instance.credits -= CatDatabase.GACHA_COST;
        GameManager.Instance.NotifyStatsChanged();

        StartCoroutine(GachaRoll());
        return true;
    }

    IEnumerator GachaRoll()
    {
        isRolling = true;
        ResetResultFX();

        if (drawButton != null)
        {
            drawButton.interactable = false;
            var bg = drawButton.GetComponent<Image>();
            if (bg != null) bg.color = buttonRollingColor;
        }
        if (drawButtonText != null)
        {
            if (!textColorCached)
            {
                originalTextColor = drawButtonText.color;
                textColorCached = true;
            }
            drawButtonText.color = buttonRollingColor;
        }

        if (resultNameText != null)
            resultNameText.text = "";

        int finalCatId = CatDatabase.Instance.RollGachaId();
        int n = slotSlots.Length;

        // 초기 배치
        for (int i = 0; i < n; i++)
        {
            if (slotSlots[i] == null) continue;
            int randomId = Random.Range(0, CatDatabase.TOTAL_COUNT);
            CatData rd = CatDatabase.Instance.Get(randomId);
            if (rd != null && rd.sprite != null)
                slotSlots[i].sprite = rd.sprite;
            slotSlots[i].rectTransform.anchoredPosition = new Vector2(0, slotHeight * i);
        }

        // 총 이동 거리 = totalCycles * slotHeight
        float totalDistance = slotHeight * totalCycles;
        int cycleCount = 0;
        int finalCycleIndex = totalCycles - n + 1;

        float elapsed = 0f;
        float prevScrolled = 0f;

        while (elapsed < totalDuration)
        {
            float t = elapsed / totalDuration;

            // 진짜 이징: 누적 거리를 시간 함수로 직접 계산
            // easeOutCubic: 처음 빠르고 끝에서 매우 느림
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float currentScrolled = totalDistance * eased;
            float deltaScroll = currentScrolled - prevScrolled;
            prevScrolled = currentScrolled;

            // 모든 슬롯 아래로
            for (int i = 0; i < n; i++)
            {
                if (slotSlots[i] == null) continue;
                Vector2 pos = slotSlots[i].rectTransform.anchoredPosition;
                pos.y -= deltaScroll;
                slotSlots[i].rectTransform.anchoredPosition = pos;
            }

            // 화면 아래로 빠진 슬롯 → 위로 재진입
            for (int i = 0; i < n; i++)
            {
                if (slotSlots[i] == null) continue;

                if (slotSlots[i].rectTransform.anchoredPosition.y <= -slotHeight)
                {
                    // 가장 위 슬롯 찾기
                    float maxY = float.MinValue;
                    for (int j = 0; j < n; j++)
                    {
                        if (slotSlots[j] == null || j == i) continue;
                        float yj = slotSlots[j].rectTransform.anchoredPosition.y;
                        if (yj > maxY) maxY = yj;
                    }

                    Vector2 newPos = slotSlots[i].rectTransform.anchoredPosition;
                    newPos.y = maxY + slotHeight;
                    slotSlots[i].rectTransform.anchoredPosition = newPos;

                    cycleCount++;

                    int catIdToShow;
                    if (cycleCount == finalCycleIndex)
                        catIdToShow = finalCatId;
                    else
                        catIdToShow = Random.Range(0, CatDatabase.TOTAL_COUNT);

                    CatData d = CatDatabase.Instance.Get(catIdToShow);
                    if (d != null && d.sprite != null)
                        slotSlots[i].sprite = d.sprite;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 마지막 스냅: 가장 중앙에 가까운 슬롯을 y=0으로
        Image centerSlot = null;
        float minDist = float.MaxValue;
        foreach (var s in slotSlots)
        {
            if (s == null) continue;
            float d = Mathf.Abs(s.rectTransform.anchoredPosition.y);
            if (d < minDist)
            {
                minDist = d;
                centerSlot = s;
            }
        }

        if (centerSlot != null)
        {
            CatData fdata = CatDatabase.Instance.Get(finalCatId);
            if (fdata != null && fdata.sprite != null)
                centerSlot.sprite = fdata.sprite;

            int slotIndex = System.Array.IndexOf(slotSlots, centerSlot);
            for (int i = 0; i < n; i++)
            {
                if (slotSlots[i] == null) continue;
                int rel = (i - slotIndex + n) % n;
                slotSlots[i].rectTransform.anchoredPosition = new Vector2(0, slotHeight * rel);
            }
        }

        // 도감 추가 - 중복이면 GACHA_REFUND만큼 환급
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnGachaDrawn();

        bool isNew = CatManager.Instance.AddFromGacha(finalCatId);
        if (!isNew && CatDatabase.GACHA_REFUND > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.credits += CatDatabase.GACHA_REFUND;
            GameManager.Instance.NotifyStatsChanged();
        }

        CatData fd = CatDatabase.Instance.Get(finalCatId);
        if (resultNameText != null && fd != null)
        {
            resultNameText.text = Loc.Get(fd.nameKey);
            resultNameText.color = CatDatabase.GetRarityColor(fd.rarity);
        }

        // ===== 결과 연출 (등급 차등 + NEW/환급 뱃지) =====
        if (fd != null)
            PlayResultFX(fd.rarity, isNew);

        if (drawButton != null)
        {
            var bg = drawButton.GetComponent<Image>();
            if (bg != null) bg.color = buttonNormalColor;
        }
        if (drawButtonText != null && textColorCached)
            drawButtonText.color = originalTextColor;

        isRolling = false;
    }

    // ============ 결과 연출 ============

    void PlayResultFX(CatRarity rarity, bool isNew)
    {
        // 뱃지 (NEW! / +환급 CR)
        if (resultBadgeText != null)
        {
            if (isNew)
                fxCoroutines.Add(StartCoroutine(BadgeNew()));
            else if (CatDatabase.GACHA_REFUND > 0)
                fxCoroutines.Add(StartCoroutine(BadgeRefund()));
        }

        if (rarity == CatRarity.Rare)
        {
            fxCoroutines.Add(StartCoroutine(PopName(1.4f, 0.25f)));
            SpawnShards(8, SHARD_BLUE, 220f);
        }
        else if (rarity == CatRarity.Legendary)
        {
            if (flashOverlay != null)
                fxCoroutines.Add(StartCoroutine(Flash(0.8f, 0.3f)));
            fxCoroutines.Add(StartCoroutine(PopName(1.6f, 0.3f)));
            fxCoroutines.Add(StartCoroutine(BlinkNameGold(1.5f)));
            fxCoroutines.Add(StartCoroutine(ShakeSlot(0.3f, 3f)));
            SpawnShards(16, GOLD, 340f);
        }
        // Common: 연출 없음
    }

    /// <summary>새 뽑기 시작 시 이전 연출 정리.</summary>
    void ResetResultFX()
    {
        foreach (var c in fxCoroutines)
            if (c != null) StopCoroutine(c);
        fxCoroutines.Clear();

        foreach (var go in activeShards)
            if (go != null) Destroy(go);
        activeShards.Clear();

        if (resultNameText != null)
            resultNameText.rectTransform.localScale = Vector3.one;
        if (resultBadgeText != null)
        {
            resultBadgeText.text = "";
            if (badgePosCached)
                resultBadgeText.rectTransform.anchoredPosition = badgeOriginalPos;
        }
        if (flashOverlay != null)
        {
            var c = flashOverlay.color; c.a = 0f; flashOverlay.color = c;
        }
        if (slotMask != null && slotMaskPosCached)
            slotMask.anchoredPosition = slotMaskOriginalPos;
    }

    /// <summary>이름 텍스트 펑: startScale → 1.0 (easeOutBack 느낌).</summary>
    IEnumerator PopName(float startScale, float duration)
    {
        if (resultNameText == null) yield break;
        var rt = resultNameText.rectTransform;
        float t = 0f;
        while (t < duration)
        {
            float p = t / duration;
            // easeOutCubic
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float scale = Mathf.LerpUnclamped(startScale, 1f, eased);
            rt.localScale = Vector3.one * scale;
            t += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    /// <summary>전설: 이름 금색↔흰색 깜빡 후 원래 등급색으로.</summary>
    IEnumerator BlinkNameGold(float duration)
    {
        if (resultNameText == null) yield break;
        Color rarityColor = resultNameText.color;
        float t = 0f;
        while (t < duration)
        {
            float p = Mathf.PingPong(t * 6f, 1f);
            resultNameText.color = Color.Lerp(GOLD, Color.white, p);
            t += Time.deltaTime;
            yield return null;
        }
        resultNameText.color = rarityColor;
    }

    /// <summary>풀스크린 플래시: startAlpha → 0.</summary>
    IEnumerator Flash(float startAlpha, float duration)
    {
        if (flashOverlay == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            var c = flashOverlay.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t / duration);
            flashOverlay.color = c;
            t += Time.deltaTime;
            yield return null;
        }
        var done = flashOverlay.color; done.a = 0f; flashOverlay.color = done;
    }

    /// <summary>슬롯 마스크 미세 진동.</summary>
    IEnumerator ShakeSlot(float duration, float strength)
    {
        if (slotMask == null) yield break;
        if (!slotMaskPosCached)
        {
            slotMaskOriginalPos = slotMask.anchoredPosition;
            slotMaskPosCached = true;
        }
        float t = 0f;
        while (t < duration)
        {
            slotMask.anchoredPosition = slotMaskOriginalPos
                + new Vector2(Random.Range(-strength, strength), Random.Range(-strength, strength));
            t += Time.deltaTime;
            yield return null;
        }
        slotMask.anchoredPosition = slotMaskOriginalPos;
    }

    /// <summary>신규 획득 뱃지: 노란 NEW! 깜빡 2회 후 고정.</summary>
    IEnumerator BadgeNew()
    {
        CacheBadgePos();
        resultBadgeText.text = "NEW!";
        resultBadgeText.color = GOLD;

        float t = 0f;
        while (t < 0.5f)
        {
            // 0.5초 동안 2회 깜빡 (알파 토글)
            var c = resultBadgeText.color;
            c.a = (Mathf.FloorToInt(t * 8f) % 2 == 0) ? 1f : 0.2f;
            resultBadgeText.color = c;
            t += Time.deltaTime;
            yield return null;
        }
        var fc = resultBadgeText.color; fc.a = 1f; resultBadgeText.color = fc;
    }

    /// <summary>중복 환급 뱃지: +N CR이 아래에서 떠오르며 등장.</summary>
    IEnumerator BadgeRefund()
    {
        CacheBadgePos();
        resultBadgeText.text = $"+{CatDatabase.GACHA_REFUND} CR";
        resultBadgeText.color = REFUND_GREEN;

        var rt = resultBadgeText.rectTransform;
        Vector2 from = badgeOriginalPos + new Vector2(0, -10f);
        float t = 0f;
        while (t < 0.3f)
        {
            float p = t / 0.3f;
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            rt.anchoredPosition = Vector2.Lerp(from, badgeOriginalPos, eased);
            var c = resultBadgeText.color; c.a = eased; resultBadgeText.color = c;
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = badgeOriginalPos;
        var fc = resultBadgeText.color; fc.a = 1f; resultBadgeText.color = fc;
    }

    void CacheBadgePos()
    {
        if (!badgePosCached && resultBadgeText != null)
        {
            badgeOriginalPos = resultBadgeText.rectTransform.anchoredPosition;
            badgePosCached = true;
        }
    }

    // ============ 픽셀 파편 ============

    /// <summary>슬롯 중앙에서 사방으로 퍼지는 픽셀 파편 생성.</summary>
    void SpawnShards(int count, Color color, float speed)
    {
        if (slotMask == null) return;
        Transform parent = slotMask.parent != null ? slotMask.parent : slotMask.transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("GachaShard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.sprite = GetShardSprite();
            img.color = color;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(8f, 8f);
            rt.anchoredPosition = slotMask.anchoredPosition;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float spd = speed * Random.Range(0.6f, 1.2f);
            Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spd;

            activeShards.Add(go);
            fxCoroutines.Add(StartCoroutine(ShardFly(go, rt, img, vel)));
        }
    }

    IEnumerator ShardFly(GameObject go, RectTransform rt, Image img, Vector2 velocity)
    {
        const float life = 0.5f;
        const float gravity = 600f;
        float t = 0f;
        Color baseColor = img.color;

        while (t < life)
        {
            if (go == null) yield break;
            velocity.y -= gravity * Time.deltaTime;
            rt.anchoredPosition += velocity * Time.deltaTime;

            var c = baseColor;
            c.a = 1f - (t / life);
            img.color = c;

            t += Time.deltaTime;
            yield return null;
        }
        if (go != null)
        {
            activeShards.Remove(go);
            Destroy(go);
        }
    }

    /// <summary>4×4 흰색 픽셀 스프라이트 (한 번만 생성, 공유).</summary>
    static Sprite GetShardSprite()
    {
        if (shardSprite != null) return shardSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        shardSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
        return shardSprite;
    }
}
