using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 업그레이드 버튼. Prefab으로 만들어서 UpgradePanel에 배치.
/// </summary>
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
            nameText.text = $"{data.icon} {data.name}";
        if (levelText != null)
            levelText.text = $"Lv.{level}";
        if (descText != null)
            descText.text = data.description;
        if (costText != null)
        {
            costText.text = $"{GameManager.FormatNumber(cost)} CR";
            costText.color = canAfford ? affordableTextColor : unaffordableTextColor;
        }

        if (buyButton != null)
            buyButton.interactable = canAfford;
        if (buttonBackground != null)
            buttonBackground.color = canAfford ? affordableColor : unaffordableColor;
    }

    void OnBuyClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BuyUpgrade(upgradeType);
        }
    }
}
