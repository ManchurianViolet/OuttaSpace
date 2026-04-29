using UnityEngine;
using System.Collections.Generic;

public enum CatRarity { Common, Rare, Legendary }

[System.Serializable]
public class CatData
{
    public int id;
    public string nameKey;
    public CatRarity rarity;
    public Sprite sprite;
}

public class CatDatabase : MonoBehaviour
{
    public static CatDatabase Instance { get; private set; }

    [Header("Cat Sprites (39 total: 27 Common + 9 Rare + 3 Legendary)")]
    [Tooltip("순서대로 0-26=일반, 27-35=희귀, 36-38=전설")]
    public Sprite[] catSprites = new Sprite[39];

    public const int COMMON_COUNT = 27;
    public const int RARE_COUNT = 9;
    public const int LEGENDARY_COUNT = 3;
    public const int TOTAL_COUNT = 39;

    public const int STARTER_CAT_ID = 0;

    public const float PROB_COMMON = 0.80f;
    public const float PROB_RARE = 0.15f;
    public const float PROB_LEGENDARY = 0.05f;

    public const int GACHA_COST = 1;        // 테스트용 1
    public const int GACHA_REFUND = 0;       // 환급도 0 (1원이라)

    private CatData[] catDataCache;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildCache();
    }

    void BuildCache()
    {
        catDataCache = new CatData[TOTAL_COUNT];
        for (int i = 0; i < TOTAL_COUNT; i++)
        {
            CatRarity rarity;
            string nameKey;

            if (i < COMMON_COUNT)
            {
                rarity = CatRarity.Common;
                nameKey = $"cat_common_{(i + 1):D2}";
            }
            else if (i < COMMON_COUNT + RARE_COUNT)
            {
                rarity = CatRarity.Rare;
                nameKey = $"cat_rare_{(i - COMMON_COUNT + 1):D2}";
            }
            else
            {
                rarity = CatRarity.Legendary;
                nameKey = $"cat_legendary_{(i - COMMON_COUNT - RARE_COUNT + 1):D2}";
            }

            catDataCache[i] = new CatData
            {
                id = i,
                nameKey = nameKey,
                rarity = rarity,
                sprite = (i < catSprites.Length) ? catSprites[i] : null
            };
        }
    }

    public CatData Get(int id)
    {
        if (id < 0 || id >= TOTAL_COUNT) return null;
        return catDataCache[id];
    }

    public static int GetRarityStartIndex(CatRarity rarity)
    {
        switch (rarity)
        {
            case CatRarity.Common: return 0;
            case CatRarity.Rare: return COMMON_COUNT;
            case CatRarity.Legendary: return COMMON_COUNT + RARE_COUNT;
        }
        return 0;
    }

    public static int GetRarityCount(CatRarity rarity)
    {
        switch (rarity)
        {
            case CatRarity.Common: return COMMON_COUNT;
            case CatRarity.Rare: return RARE_COUNT;
            case CatRarity.Legendary: return LEGENDARY_COUNT;
        }
        return 0;
    }

    public int RollGachaId()
    {
        float roll = Random.value;
        CatRarity rarity;
        if (roll < PROB_LEGENDARY)
            rarity = CatRarity.Legendary;
        else if (roll < PROB_LEGENDARY + PROB_RARE)
            rarity = CatRarity.Rare;
        else
            rarity = CatRarity.Common;

        int start = GetRarityStartIndex(rarity);
        int count = GetRarityCount(rarity);
        return start + Random.Range(0, count);
    }

    public static Color GetRarityColor(CatRarity rarity)
    {
        switch (rarity)
        {
            case CatRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
            case CatRarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case CatRarity.Legendary: return new Color(1f, 0.6f, 0.1f);
        }
        return Color.white;
    }
}