using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 고양이 도감 UI.
/// - 3x3 그리드, 5페이지 (일반 3장, 희귀 1장, 전설 1장)
/// - 보유: 컬러 + 클릭 가능
/// - 미보유: 검은 실루엣 (코드로 변환)
/// - 현재 사용 중: 무지개 테두리
/// - 클릭 시 고양이 교체 + 팝업 닫기 + 창 축소
/// 
/// Panel에 붙이고 Inspector에서 다 연결.
/// </summary>
public class CatCollectionUI : MonoBehaviour
{
    [Header("Page")]
    public TextMeshProUGUI pageTitleText;
    public Button prevButton;
    public Button nextButton;

    [Header("Grid Slots (9)")]
    public CatSlot[] slots = new CatSlot[9];

    [Header("Close Button")]
    public Button closeButton;

    private int currentPage = 0;     // 0~4
    private const int SLOTS_PER_PAGE = 9;

    // 실루엣 스프라이트 캐시 (원본 스프라이트 → 실루엣)
    private System.Collections.Generic.Dictionary<Sprite, Sprite> silhouetteCache
        = new System.Collections.Generic.Dictionary<Sprite, Sprite>();

    void OnEnable()
    {
        // 항해 중이면 창 확장
        WindowStateManager wsm = WindowStateManager.Instance;
        if (wsm != null && wsm.currentState == WindowStateManager.WindowState.Traveling)
        {
            wsm.ExpandForCollection();
        }

        currentPage = 0;
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
        // 현재 사용 중 고양이 슬롯에 표시 (배경 검정 + bobbing은 슬롯 자체가 처리)
        int cm = (CatManager.Instance != null) ? CatManager.Instance.currentCatId : -1;
        for (int i = 0; i < slots.Length; i++)
        {
            int catId = currentPage * SLOTS_PER_PAGE + i;
            if (slots[i] != null)
                slots[i].SetCurrent(catId == cm);
        }
    }

    void OnPrev()
    {
        if (currentPage > 0) { currentPage--; RefreshPage(); UpdateNav(); }
    }

    void OnNext()
    {
        if (currentPage < 4) { currentPage++; RefreshPage(); UpdateNav(); }
    }

    void OnClose()
    {
        WindowStateManager wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.CollapseFromCollection();

        gameObject.SetActive(false);
    }

    void OnCatsChanged() => RefreshPage();
    void OnLanguageChanged() => UpdateNav();
    void OnCurrentChanged(int id) => RefreshPage();

    void UpdateNav()
    {
        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < 4;

        if (pageTitleText != null)
        {
            string title;
            if (currentPage < 3) title = Loc.Get("rarity_common");
            else if (currentPage == 3) title = Loc.Get("rarity_rare");
            else title = Loc.Get("rarity_legendary");
            pageTitleText.text = $"{title} ({currentPage + 1}/5)";
        }
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

            if (catId >= CatDatabase.TOTAL_COUNT)
            {
                slots[i].Clear();
                continue;
            }

            CatData data = db.Get(catId);
            bool owned = cm.IsOwned(catId);
            Sprite displaySprite;

            if (owned)
            {
                displaySprite = data.sprite;
            }
            else
            {
                displaySprite = GetSilhouette(data.sprite);
            }

            slots[i].Setup(catId, displaySprite, owned);
        }
    }

    /// <summary>
    /// 컬러 스프라이트 → 검은 실루엣 스프라이트 (캐시)
    /// </summary>
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
