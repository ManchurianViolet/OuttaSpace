using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감에서 고양이 슬롯 위에 마우스 호버 시 뜨는 작은 이름 툴팁.
/// 싱글톤, 자동 생성.
///
/// 보더는 9-slice sprite 대신 별도의 Image 4개(상하좌우)로 그려서
/// 박스 크기가 작거나 클 때도 안정적으로 보임.
/// </summary>
public class CatTooltip : MonoBehaviour
{
    public static CatTooltip Instance { get; private set; }

    [Header("Layout")]
    public Vector2 padding = new Vector2(12, 6);
    public Vector2 offsetFromSlot = new Vector2(0, 8);
    public float widthSafetyMargin = 8f;

    [Header("Style")]
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.10f, 0.95f);
    public Color borderColor = Color.white;
    public int borderWidth = 1;
    public int nameFontSize = 16;

    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Image background;
    private TextMeshProUGUI nameText;
    private Image borderTop, borderBottom, borderLeft, borderRight;

    public static CatTooltip GetOrCreate(Canvas parentCanvas)
    {
        if (Instance != null) return Instance;
        if (parentCanvas == null) return null;

        GameObject obj = new GameObject("CatTooltip",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(CatTooltip));
        obj.transform.SetParent(parentCanvas.transform, worldPositionStays: false);
        obj.transform.SetAsLastSibling();

        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildUI();
        Hide();
    }

    void BuildUI()
    {
        rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 배경 — 단색 fill만 (보더는 별도)
        background = GetComponent<Image>();
        background.raycastTarget = false;
        background.sprite = null; // 기본 흰색 사각형
        background.color = backgroundColor;

        // 보더 4개 생성
        borderTop = CreateBorderEdge("BorderTop");
        borderBottom = CreateBorderEdge("BorderBottom");
        borderLeft = CreateBorderEdge("BorderLeft");
        borderRight = CreateBorderEdge("BorderRight");

        SetupBorderAnchors();

        // 이름 텍스트
        GameObject nameObj = new GameObject("Name",
            typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(transform, worldPositionStays: false);
        nameText = nameObj.GetComponent<TextMeshProUGUI>();
        nameText.fontSize = nameFontSize;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Overflow;

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = Vector2.zero;
        nameRt.anchorMax = Vector2.one;
        nameRt.offsetMin = new Vector2(padding.x, padding.y);
        nameRt.offsetMax = new Vector2(-padding.x, -padding.y);
    }

    Image CreateBorderEdge(string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, worldPositionStays: false);

        Image img = obj.GetComponent<Image>();
        img.sprite = null;
        img.color = borderColor;
        img.raycastTarget = false;
        return img;
    }

    void SetupBorderAnchors()
    {
        // 상단 - 가로 전체, 위쪽 borderWidth 두께
        RectTransform topRt = borderTop.rectTransform;
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = new Vector2(1, 1);
        topRt.pivot = new Vector2(0.5f, 1);
        topRt.offsetMin = new Vector2(0, -borderWidth);
        topRt.offsetMax = new Vector2(0, 0);

        // 하단
        RectTransform botRt = borderBottom.rectTransform;
        botRt.anchorMin = new Vector2(0, 0);
        botRt.anchorMax = new Vector2(1, 0);
        botRt.pivot = new Vector2(0.5f, 0);
        botRt.offsetMin = new Vector2(0, 0);
        botRt.offsetMax = new Vector2(0, borderWidth);

        // 좌측 - 세로 전체, 왼쪽 borderWidth 두께
        RectTransform leftRt = borderLeft.rectTransform;
        leftRt.anchorMin = new Vector2(0, 0);
        leftRt.anchorMax = new Vector2(0, 1);
        leftRt.pivot = new Vector2(0, 0.5f);
        leftRt.offsetMin = new Vector2(0, 0);
        leftRt.offsetMax = new Vector2(borderWidth, 0);

        // 우측
        RectTransform rightRt = borderRight.rectTransform;
        rightRt.anchorMin = new Vector2(1, 0);
        rightRt.anchorMax = new Vector2(1, 1);
        rightRt.pivot = new Vector2(1, 0.5f);
        rightRt.offsetMin = new Vector2(-borderWidth, 0);
        rightRt.offsetMax = new Vector2(0, 0);
    }

    public void ShowFor(int catId, RectTransform slotRect, bool owned, bool locked = false)
    {
        if (CatDatabase.Instance == null || slotRect == null) return;

        CatData cat = CatDatabase.Instance.Get(catId);
        if (cat == null) { Hide(); return; }

        if (locked)
        {
            nameText.text = Loc.Get("coming_soon");
            nameText.color = new Color(1f, 0.85f, 0.4f); // 노랑 (눈에 띄게)
        }
        else if (owned)
        {
            nameText.text = Loc.Get(cat.nameKey);
            nameText.color = CatDatabase.GetRarityColor(cat.rarity);
        }
        else
        {
            nameText.text = "???";
            nameText.color = new Color(0.5f, 0.5f, 0.5f);
        }

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        Canvas.ForceUpdateCanvases();
        PositionAboveSlot(slotRect);
    }

    void PositionAboveSlot(RectTransform slotRect)
    {
        Canvas parentCanvas = rt.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform canvasRT = parentCanvas.transform as RectTransform;
        if (canvasRT == null) return;

        nameText.ForceMeshUpdate();

        Vector2 preferred = nameText.GetPreferredValues(nameText.text);
        float renderedWidth = nameText.textBounds.size.x;
        float textWidth = Mathf.Max(Mathf.Ceil(preferred.x), Mathf.Ceil(renderedWidth));

        Vector2 boxSize = new Vector2(
            textWidth + padding.x * 2 + widthSafetyMargin,
            nameFontSize + padding.y * 2
        );
        rt.sizeDelta = boxSize;

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;

        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : parentCanvas.worldCamera;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(cam, topCenterWorld),
            cam,
            out localPoint);

        Vector2 pos = localPoint + new Vector2(offsetFromSlot.x, offsetFromSlot.y);

        float canvasW = canvasRT.rect.width;
        float canvasH = canvasRT.rect.height;
        float halfW = boxSize.x * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -canvasW * 0.5f + halfW, canvasW * 0.5f - halfW);

        float topEdge = pos.y + boxSize.y;
        if (topEdge > canvasH * 0.5f)
        {
            Vector3 bottomCenterWorld = (corners[0] + corners[3]) * 0.5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT,
                RectTransformUtility.WorldToScreenPoint(cam, bottomCenterWorld),
                cam,
                out localPoint);
            rt.pivot = new Vector2(0.5f, 1f);
            pos = localPoint + new Vector2(offsetFromSlot.x, -offsetFromSlot.y);
        }
        else
        {
            rt.pivot = new Vector2(0.5f, 0f);
        }

        rt.anchoredPosition = pos;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
