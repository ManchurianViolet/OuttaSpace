using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 고양이 도감 UI.
/// - 3x3 그리드, 5페이지 (일반 3장=27마리, 희귀 1장=9마리, 전설 1장=3마리)
/// - 페이지 타이틀의 (n/m): n=현재 등급의 보유 수, m=현재 등급의 전체 수
///   예: 일반 (12/27), 희귀 (3/9), 전설 (2/3)
/// - 하단 페이지 번호: 단순히 1~5
/// - 보유: 컬러 + 클릭 가능
/// - 미보유: 검은 실루엣 (코드로 변환)
/// - 현재 사용 중: 슬롯 배경 검정 + 별 흐름 + bobbing
/// - 클릭 시 고양이 교체 + 팝업 닫기 + 창 축소
///
/// Panel에 붙이고 Inspector에서 다 연결.
/// </summary>
public class CatCollectionUI : MonoBehaviour
{
    [Header("Page")]
    public TextMeshProUGUI pageTitleText;
    [Tooltip("하단의 작은 페이지 번호 (1~5).")]
    public TextMeshProUGUI pageNumberText;
    public Button prevButton;
    public Button nextButton;

    [Header("Grid Slots (9)")]
    public CatSlot[] slots = new CatSlot[9];

    [Header("Close Button")]
    public Button closeButton;

    private int currentPage = 0;     // 0~4
    private const int SLOTS_PER_PAGE = 9;
    private const int TOTAL_PAGES = 5;

    private System.Collections.Generic.Dictionary<Sprite, Sprite> silhouetteCache
        = new System.Collections.Generic.Dictionary<Sprite, Sprite>();

    void OnEnable()
    {
        WindowStateManager wsm = WindowStateManager.Instance;
        if (wsm != null && wsm.currentState == WindowStateManager.WindowState.Traveling)
        {
            wsm.ExpandForCollection();
        }

        // 현재 사용 중인 고양이가 있는 페이지로 시작
        currentPage = 0;
        if (CatManager.Instance != null)
        {
            int currentCatId = CatManager.Instance.currentCatId;
            if (currentCatId >= 0 && currentCatId < CatDatabase.TOTAL_COUNT)
                currentPage = currentCatId / SLOTS_PER_PAGE;
        }
        RefreshPage();
        UpdateNav();

        if (prevButton != null)
            prevButton.onClick.AddListener(OnPrev);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        if (CatManager.Instance != null)
        {
            CatManager.Instance.OnCatsChanged += OnCatsChanged;
            CatManager.Instance.OnCatChanged += OnCurrentChanged;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged += OnLanguageChanged;
    }

    void OnDisable()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(OnPrev);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
        if (closeButton != null) closeButton.onClick.RemoveListener(OnClose);

        if (CatManager.Instance != null)
        {
            CatManager.Instance.OnCatsChanged -= OnCatsChanged;
            CatManager.Instance.OnCatChanged -= OnCurrentChanged;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged -= OnLanguageChanged;
    }

    void Update()
    {
        int cm = (CatManager.Instance != null) ? CatManager.Instance.currentCatId : -1;
        for (int i = 0; i < slots.Length; i++)
        {
            int catId = currentPage * SLOTS_PER_PAGE + i;
            if (slots[i] != null && slots[i].gameObject.activeSelf)
                slots[i].SetCurrent(catId == cm);
        }
    }

    void OnPrev()
    {
        if (currentPage > 0) { currentPage--; RefreshPage(); UpdateNav(); }
    }

    void OnNext()
    {
        if (currentPage < TOTAL_PAGES - 1) { currentPage++; RefreshPage(); UpdateNav(); }
    }

    void OnClose()
    {
        WindowStateManager wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.CollapseFromCollection();

        gameObject.SetActive(false);
    }

    void OnCatsChanged() { RefreshPage(); UpdateNav(); }
    void OnLanguageChanged() => UpdateNav();
    void OnCurrentChanged(int id) => RefreshPage();

    void UpdateNav()
    {
        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < TOTAL_PAGES - 1;

        if (pageTitleText != null)
        {
            string title;
            Color titleColor;
            CatRarity rarity;

            if (currentPage < 3)
            {
                title = Loc.Get("rarity_common");
                titleColor = Color.white;
                rarity = CatRarity.Common;
            }
            else if (currentPage == 3)
            {
                title = Loc.Get("rarity_rare");
                titleColor = CatDatabase.GetRarityColor(CatRarity.Rare);
                rarity = CatRarity.Rare;
            }
            else
            {
                title = Loc.Get("rarity_legendary");
                titleColor = CatDatabase.GetRarityColor(CatRarity.Legendary);
                rarity = CatRarity.Legendary;
            }

            int total = GetAvailableCount(rarity);
            int owned = CountOwnedByRarity(rarity);

            pageTitleText.text = $"{title} ({owned}/{total})";
            pageTitleText.color = titleColor;
        }

        // 하단 페이지 번호 (1~5)
        if (pageNumberText != null)
            pageNumberText.text = (currentPage + 1).ToString();
    }

    /// <summary>
    /// 해당 등급의 보유 수 / 전체 수 (잠긴 데모 ID는 제외).
    /// </summary>
    int CountOwnedByRarity(CatRarity rarity)
    {
        if (CatManager.Instance == null) return 0;

        int start = CatDatabase.GetRarityStartIndex(rarity);
        int count = CatDatabase.GetRarityCount(rarity);
        int owned = 0;
        for (int i = 0; i < count; i++)
        {
            int id = start + i;
            if (CatManager.Instance.IsOwned(id)) owned++;
        }
        return owned;
    }

    /// <summary>
    /// 등급별 전체 수.
    /// </summary>
    int GetAvailableCount(CatRarity rarity)
    {
        return CatDatabase.GetRarityCount(rarity);
    }

    void RefreshPage()
    {
        var db = CatDatabase.Instance;
        var cm = CatManager.Instance;
        if (db == null || cm == null) return;

        for (int i = 0; i < SLOTS_PER_PAGE; i++)
        {
            int catId = currentPage * SLOTS_PER_PAGE + i;
            if (slots[i] == null) continue;

            // 전설 페이지의 빈 슬롯(catId 39~44) → Coming Soon 표시
            // 정식 출시 시 새 전설 추가되면 자동으로 해당 catId만큼 실제 슬롯으로 전환됨
            if (catId >= CatDatabase.TOTAL_COUNT)
            {
                // 전설 페이지(currentPage == 4 — 슬롯 36~44) 안에서만 placeholder
                if (currentPage == 4)
                {
                    if (!slots[i].gameObject.activeSelf)
                        slots[i].gameObject.SetActive(true);
                    // 첫 전설 고양이 실루엣을 placeholder로 사용 (실루엣 변환됨)
                    Sprite placeholder = GetPlaceholderSilhouette();
                    slots[i].Setup(catId, placeholder, false, locked: true);
                }
                else
                {
                    slots[i].gameObject.SetActive(false);
                }
                continue;
            }

            if (!slots[i].gameObject.activeSelf)
                slots[i].gameObject.SetActive(true);

            CatData data = db.Get(catId);
            bool locked = false;  // 정식판: 모든 고양이 잠금 없음
            bool owned = cm.IsOwned(catId);
            Sprite displaySprite;

            if (locked)
            {
                // 잠금: 회색 실루엣 (스프라이트 있어도 회색 처리)
                displaySprite = (data != null && data.sprite != null)
                    ? GetSilhouette(data.sprite) : null;
            }
            else if (owned)
                displaySprite = data.sprite;
            else
                displaySprite = GetSilhouette(data.sprite);

            slots[i].Setup(catId, displaySprite, owned, locked);
        }
    }

    Sprite GetSilhouette(Sprite original)
    {
        if (original == null) return null;
        if (silhouetteCache.ContainsKey(original))
            return silhouetteCache[original];

        Texture2D src = original.texture;
        Rect rect = original.rect;
        int w = (int)rect.width;
        int h = (int)rect.height;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] srcPixels = src.GetPixels((int)rect.x, (int)rect.y, w, h);
        Color[] dstPixels = new Color[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            if (srcPixels[i].a > 0.05f)
                dstPixels[i] = new Color(0.08f, 0.08f, 0.1f, srcPixels[i].a);
            else
                dstPixels[i] = Color.clear;
        }

        tex.SetPixels(dstPixels);
        tex.Apply();

        Sprite sil = Sprite.Create(tex, new Rect(0, 0, w, h), original.pivot / new Vector2(w, h), original.pixelsPerUnit);
        silhouetteCache[original] = sil;
        return sil;
    }

    /// <summary>
    /// 전설 페이지의 빈 슬롯에 표시할 placeholder 실루엣.
    /// 첫 전설 고양이(catId 36 = Rudolph)의 실루엣을 재활용.
    /// </summary>
    Sprite GetPlaceholderSilhouette()
    {
        var db = CatDatabase.Instance;
        if (db == null) return null;
        int firstLegendaryId = CatDatabase.GetRarityStartIndex(CatRarity.Legendary);
        CatData firstLegendary = db.Get(firstLegendaryId);
        if (firstLegendary == null || firstLegendary.sprite == null) return null;
        return GetSilhouette(firstLegendary.sprite);
    }

    void OnDestroy()
    {
        foreach (var kvp in silhouetteCache)
        {
            if (kvp.Value != null && kvp.Value.texture != null)
                Destroy(kvp.Value.texture);
        }
        silhouetteCache.Clear();
    }
}
