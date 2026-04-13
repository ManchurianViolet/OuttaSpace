public enum UpgradeType { Engine, Fuel, Nav, Warp }

[System.Serializable]
public class UpgradeData
{
    public UpgradeType type;
    public string name;
    public string icon;
    public string description;
    public double baseCost;
    public double costMult;
    public double baseEffect;   // km/s 추가량
    public double effectMult;
}

public static class UpgradeDatabase
{
    private static readonly UpgradeData[] upgrades = new UpgradeData[]
    {
        new UpgradeData {
            type = UpgradeType.Engine, name = "엔진", icon = "◆",
            description = "기본 추진력 향상",
            baseCost = 10, costMult = 1.8,
            baseEffect = 50, effectMult = 1.4      // +50 km/s per level
        },
        new UpgradeData {
            type = UpgradeType.Fuel, name = "연료 셀", icon = "▣",
            description = "연료 효율 증가",
            baseCost = 25, costMult = 2.0,
            baseEffect = 20, effectMult = 1.5       // +20 km/s per level
        },
        new UpgradeData {
            type = UpgradeType.Nav, name = "항법 장치", icon = "◈",
            description = "경로 최적화",
            baseCost = 80, costMult = 2.2,
            baseEffect = 80, effectMult = 1.3       // +80 km/s per level
        },
        new UpgradeData {
            type = UpgradeType.Warp, name = "워프 코어", icon = "✦",
            description = "초광속 접근",
            baseCost = 500, costMult = 2.5,
            baseEffect = 500, effectMult = 1.6      // +500 km/s per level
        },
    };

    public static UpgradeData Get(UpgradeType type)
    {
        return upgrades[(int)type];
    }

    public static UpgradeData[] All => upgrades;
}
