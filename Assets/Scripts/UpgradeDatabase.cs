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
    public int maxLevel;   // 최대 레벨. 0이면 무제한.
}

public static class UpgradeDatabase
{
    private static readonly UpgradeData[] upgrades = new UpgradeData[]
    {
        // Speed: baseCost 10 × 1.26^렙 → 렙 20부터 1000 CR 고정 (10×1.26^20 ≈ 1018)
        // 효과: 등비 누적 (base 2500, mult 1.22) → L80/80 전설냥 기준 안드로메다 구간 약 10시간
        new UpgradeData {
            type = UpgradeType.Speed,
            nameKey = "upgrade_speed", descKey = "upgrade_speed_desc",
            baseCost = 10, costMult = 1.26,
            baseEffect = 2500, effectMult = 1.22,
            maxCost = 1000,
            maxLevel = 100
        },
        // BoosterSpeed: baseCost 40 × 1.175^렙 → 렙 20부터 1000 CR 고정 (40×1.175^20 ≈ 1006)
        // 효과: 선형 (effectMult=1.0 → 렙당 +0.97) → 배율 = 3 + 0.97×렙, L100 = x100
        new UpgradeData {
            type = UpgradeType.BoosterSpeed,
            nameKey = "upgrade_bspd", descKey = "upgrade_bspd_desc",
            baseCost = 40, costMult = 1.175,
            baseEffect = 0.97, effectMult = 1.0,
            maxCost = 1000,
            maxLevel = 100
        },
    };

    public static UpgradeData Get(UpgradeType type) => upgrades[(int)type];
    public static UpgradeData[] All => upgrades;
}
