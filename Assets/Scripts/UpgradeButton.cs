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
    public Color affordableColor = new Color(0.15f, 0.24f, 0.47f, 0.8f);
    public Color unaffordableColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
    public Color disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
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
        bool isDemoLocked = (upgradeType == UpgradeType.CatSwap);

        if (nameText != null)
            nameText.text = $"{data.icon} {Loc.Get(data.nameKey)}";
        if (levelText != null)
            levelText.text = isDemoLocked ? "" : $"Lv.{level}";
        if (descText != null)
            descText.text = Loc.Get(data.descKey);
        if (costText != null)
        {
            if (isDemoLocked)
            {
                costText.text = "LOCKED";
                costText.color = unaffordableTextColor;
            }
            else
            {
                costText.text = $"{GameManager.FormatNumber(cost)} CR";
                costText.color = canAfford ? affordableTextColor : unaffordableTextColor;
            }
        }

        if (buyButton != null)
            buyButton.interactable = canAfford && !isDemoLocked;
        if (buttonBackground != null)
            buttonBackground.color = isDemoLocked ? disabledColor : (canAfford ? affordableColor : unaffordableColor);
    }

    void OnBuyClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.BuyUpgrade(upgradeType);
    }
}
