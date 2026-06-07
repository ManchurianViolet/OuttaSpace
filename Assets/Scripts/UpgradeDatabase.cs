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
    public double maxCost; // 비용 상한. 이 값에 도달하면 더 비싸지지 않음. 0이면 무제한.
}

public static class UpgradeDatabase
{
    private static readonly UpgradeData[] upgrades = new UpgradeData[]
    {
        // Speed: 초반 지수 상승 → Lv 22쯤 2000 CR 도달 후 고정
        // baseCost 10, costMult 1.8 → Lv 22에서 2,003 CR
        new UpgradeData {
            type = UpgradeType.Speed,
            nameKey = "upgrade_speed", descKey = "upgrade_speed_desc",
            baseCost = 10, costMult = 1.6,
            baseEffect = 1000, effectMult = 1.6,
            maxCost = 1000
        },
        // BoosterSpeed: Speed보다 비싸게 시작, cap도 더 높음 (덜 자주 살 거)
        new UpgradeData {
            type = UpgradeType.BoosterSpeed,
            nameKey = "upgrade_bspd", descKey = "upgrade_bspd_desc",
            baseCost = 40, costMult = 1.4,
            baseEffect = 1.0, effectMult = 1.5,
            maxCost = 1000
        },
    };

    public static UpgradeData Get(UpgradeType type) => upgrades[(int)type];
    public static UpgradeData[] All => upgrades;
}
