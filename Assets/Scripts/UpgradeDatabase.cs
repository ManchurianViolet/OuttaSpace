public enum UpgradeType { Speed, BoosterSpeed }

[System.Serializable]
public class UpgradeData
{
    public UpgradeType type;
    public string nameKey;
    public string descKey;
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
            nameKey = "upgrade_speed", descKey = "upgrade_speed_desc",
            baseCost = 10, costMult = 1.8,
            baseEffect = 50, effectMult = 1.4
        },
        new UpgradeData {
            type = UpgradeType.BoosterSpeed,
            nameKey = "upgrade_bspd", descKey = "upgrade_bspd_desc",
            baseCost = 50, costMult = 2.2,
            baseEffect = 0.5, effectMult = 1.4
        },
    };

    public static UpgradeData Get(UpgradeType type) => upgrades[(int)type];
    public static UpgradeData[] All => upgrades;
}