using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 그리드의 한 칸.
/// 현재 사용 중인 고양이는: 슬롯 배경이 검게 + 별이 흐름 + 고양이 살짝 bobbing.
/// </summary>
public class CatSlot : MonoBehaviour
{
    [Header("References")]
    public Image slotBackground;   // 슬롯 회색 배경 (현재 슬롯일 때 색 전환)
    public Image catImage;
    public Button button;

    [Header("Current Cat Indicator")]
    public Color currentBgColor = new Color(0.05f, 0.05f, 0.08f, 1f); // 우주 검은빛
    public float bobAmplitude = 2f;
    public float bobSpeed = 2f;

    private int catId = -1;
    private bool isOwned;
    private bool isCurrent;

    private Color normalBgColor = Color.gray;
    private Vector2 catBasePos;
    private SlotStarfield starfield;

    void Awake()
    {
        if (slotBackground != null)
            normalBgColor = slotBackground.color;
        if (catImage != null)
            catBasePos = catImage.rectTransform.anchoredPosition;

        CreateStarfield();
    }

    void CreateStarfield()
    {
        GameObject sf = new GameObject("Starfield");
        RectTransform sfRt = sf.AddComponent<RectTransform>();
        sfRt.SetParent(transform, false);

        sfRt.anchorMin = Vector2.zero;
        sfRt.anchorMax = Vector2.one;
        sfRt.offsetMin = Vector2.zero;
        sfRt.offsetMax = Vector2.zero;

        starfield = sf.AddComponent<SlotStarfield>();

        // catImage 뒤에 그려지도록
        if (catImage != null)
            sf.transform.SetSiblingIndex(catImage.transform.GetSiblingIndex());

        sf.SetActive(false);
    }

    void Start()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        if (!isCurrent || catImage == null) return;

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        Vector2 p = catBasePos;
        p.y += offset;
        catImage.rectTransform.anchoredPosition = p;
    }

    public void Setup(int id, Sprite sprite, bool owned)
    {
        catId = id;
        isOwned = owned;

        if (catImage != null)
        {
            catImage.sprite = sprite;
            catImage.color = owned ? Color.white : new Color(1, 1, 1, 0.9f);
            catImage.enabled = (sprite != null);
        }

        if (button != null)
            button.interactable = owned;
    }

    public void Clear()
    {
        catId = -1;
        SetCurrent(false);
        if (catImage != null) catImage.enabled = false;
        if (button != null) button.interactable = false;
    }

    public void SetCurrent(bool current)
    {
        if (isCurrent == current) return;
        isCurrent = current;

        if (slotBackground != null)
            slotBackground.color = current ? currentBgColor : normalBgColor;

        if (starfield != null)
            starfield.gameObject.SetActive(current);

        if (!current && catImage != null)
            catImage.rectTransform.anchoredPosition = catBasePos;
    }

    void OnClick()
    {
        if (!isOwned || catId < 0) return;
        if (CatManager.Instance == null) return;

        CatManager.Instance.SetCurrentCat(catId);

        CatCollectionUI parent = GetComponentInParent<CatCollectionUI>();
        if (parent != null && parent.closeButton != null)
            parent.closeButton.onClick.Invoke();
    }
}