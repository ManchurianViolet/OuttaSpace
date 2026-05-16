using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Animation")]
    public float totalDuration = 5f;
    public int totalCycles = 25;

    [Header("Button Visual")]
    public Color buttonNormalColor = Color.white;
    public Color buttonRollingColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private bool isRolling;
    private Color originalTextColor = Color.white;
    private bool textColorCached;

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

        // 도감 추가 (중복이어도 환급 없음)
        CatManager.Instance.AddFromGacha(finalCatId);

        CatData fd = CatDatabase.Instance.Get(finalCatId);
        if (resultNameText != null && fd != null)
        {
            resultNameText.text = Loc.Get(fd.nameKey);
            resultNameText.color = CatDatabase.GetRarityColor(fd.rarity);
        }

        if (drawButton != null)
        {
            var bg = drawButton.GetComponent<Image>();
            if (bg != null) bg.color = buttonNormalColor;
        }
        if (drawButtonText != null && textColorCached)
            drawButtonText.color = originalTextColor;

        isRolling = false;
    }
}