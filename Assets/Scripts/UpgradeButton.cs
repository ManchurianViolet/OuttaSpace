using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    public UpgradeType upgradeType;

    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public Button buyButton;
    public Image buttonBackground;

    [Header("Colors")]
    public Color affordableTextColor = new Color(1f, 0.84f, 0f);
    public Color unaffordableTextColor = new Color(0.38f, 0.25f, 0.13f);

    void Start()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        UpgradeData data = UpgradeDatabase.Get(upgradeType);
        int level = gm.GetUpgradeLevel(upgradeType);
        double cost = gm.GetUpgradeCost(upgradeType);
        bool canAfford = gm.CanAfford(upgradeType);

        if (nameText != null)
        {
            // BoosterSpeed 업글은 명왕성 통과 후 '성간 부스터'로 라벨 변경
            string nameKey = data.nameKey;
            string descKey = data.descKey;
            if (upgradeType == UpgradeType.BoosterSpeed && gm.IsInterstellarUnlocked())
            {
                nameKey = "upgrade_bspd_interstellar";
                descKey = "upgrade_bspd_interstellar_desc";
            }
            nameText.text = Loc.Get(nameKey);
            if (descText != null) descText.text = Loc.Get(descKey);
        }
        if (levelText != null) levelText.text = $"Lv.{level}";
        if (costText != null)
        {
            costText.text = $"{GameManager.FormatNumber(cost)} CR";
            costText.color = canAfford ? affordableTextColor : unaffordableTextColor;
        }

        if (buyButton != null) buyButton.interactable = canAfford;
        if (buttonBackground != null) buttonBackground.color = Color.white;

        // �� ��� ���ڸ� ��¦ �帮��
        float textAlpha = canAfford ? 1f : 0.75f;
        if (nameText != null) { var c = nameText.color; c.a = textAlpha; nameText.color = c; }
        if (levelText != null) { var c = levelText.color; c.a = textAlpha; levelText.color = c; }
        if (descText != null) { var c = descText.color; c.a = textAlpha; descText.color = c; }
        if (costText != null) { var c = costText.color; c.a = textAlpha; costText.color = c; }
    }

    void OnBuyClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.BuyUpgrade(upgradeType);
    }
}