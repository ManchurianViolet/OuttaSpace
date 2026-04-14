public enum UpgradeType { Speed, BoosterCapacity, BoosterSpeed, CatSwap }

[System.Serializable]
public class UpgradeData
{
    public UpgradeType type;
    public string nameKey;       // Loc key
    public string descKey;       // Loc key
    public string icon;
    public double baseCost;
    public double costMult;
    public double baseEffect;
    public double effectMult;
}

public static class UpgradeDatabase
{
    private static readonly UpgradeData[] upgrades = new UpgradeData[]
    {
        new UpgradeData {
            type = UpgradeType.Speed,
            nameKey = "upgrade_speed", descKey = "upgrade_speed_desc", icon = "◆",
            baseCost = 10, costMult = 1.8,
            baseEffect = 50, effectMult = 1.4       // +50 km/s per level
        },
        new UpgradeData {
            type = UpgradeType.BoosterCapacity,
            nameKey = "upgrade_bcap", descKey = "upgrade_bcap_desc", icon = "▣",
            baseCost = 30, costMult = 2.0,
            baseEffect = 0.15, effectMult = 1.3     // +15% 용량 per level
        },
        new UpgradeData {
            type = UpgradeType.BoosterSpeed,
            nameKey = "upgrade_bspd", descKey = "upgrade_bspd_desc", icon = "✦",
            baseCost = 50, costMult = 2.2,
            baseEffect = 0.5, effectMult = 1.4      // +0.5x 배율 per level
        },
        new UpgradeData {
            type = UpgradeType.CatSwap,
            nameKey = "upgrade_cat", descKey = "upgrade_cat_desc", icon = "🐱",
            baseCost = 9999, costMult = 1,
            baseEffect = 0, effectMult = 1
        },
    };

    public static UpgradeData Get(UpgradeType type)
    {
        return upgrades[(int)type];
    }

    public static UpgradeData[] All => upgrades;
}
