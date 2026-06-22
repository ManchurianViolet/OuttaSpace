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
    public Color flameColor;
}

public class CatDatabase : MonoBehaviour
{
    public static CatDatabase Instance { get; private set; }

    [Header("Cat Sprites (36 total: 18 Common + 9 Rare + 9 Legendary)")]
    [Tooltip("순서대로 0-26=일반, 27-35=희귀, 36-38=전설")]
    public Sprite[] catSprites = new Sprite[36];

    public const int COMMON_COUNT = 18;
    public const int RARE_COUNT = 9;
    public const int LEGENDARY_COUNT = 9;
    public const int TOTAL_COUNT = 36;

    public const int STARTER_CAT_ID = 0;

    public const float PROB_COMMON = 0.80f;
    public const float PROB_RARE = 0.15f;
    public const float PROB_LEGENDARY = 0.05f;

    public const int GACHA_COST = 100;
    public const int GACHA_REFUND = 30;       // 중복 시 30% 환급

    private CatData[] catDataCache;

    /// <summary>
    /// 고양이별 평상시 엔진 불꽃 색.
    /// 이름의 톤(음식/색/털→따뜻한 색, 우주/SF→차가운 색, 천체→강렬한 색)에 맞춰 설계.
    /// 부스트 시엔 이 색이 무시되고 항상 파랑(ShipController에서 처리).
    /// </summary>
    private static readonly Color[] FLAME_COLORS = new Color[TOTAL_COUNT]
    {
        // Common 0-17 (CSV 이름 기준)
        HexC("#FF6B35"), // 00 Ginger - 오렌지빨강
        HexC("#7A7A8A"), // 01 Smokey - 스모키그레이
        HexC("#E8B04D"), // 02 Patches - 다채로운 황
        HexC("#D4A574"), // 03 Toast - 토스트 갈색
        HexC("#F5F5FA"), // 04 Snow - 화이트
        HexC("#D8D8D8"), // 05 Marble - 화이트그레이
        HexC("#8B5E3C"), // 06 Mocha - 모카 브라운
        HexC("#2E2A2A"), // 07 Oreo - 오레오 블랙
        HexC("#B07A4B"), // 08 Cookie - 쿠키 진갈
        HexC("#C9A57B"), // 09 Latte - 라떼 베이지
        HexC("#3D3D44"), // 10 Domino - 흑백 다크그레이
        HexC("#C8431F"), // 11 Maple - 단풍빨강
        HexC("#C5D1E0"), // 12 Cloud - 라이트블루그레이
        HexC("#3D3D44"), // 13 Shadow - 다크그레이
        HexC("#23232A"), // 14 Tuxedo - 턱시도 블랙
        HexC("#E8924D"), // 15 Calico - 삼색 주황
        HexC("#C29DA0"), // 16 Dusty - 더스티핑크
        HexC("#F5EFD9"), // 17 Tofu - 아이보리

        // Rare 18-26 (우주/SF/영웅)
        HexC("#5FFFE0"), // 18 Aurora - 오로라 시안초록
        HexC("#FF3838"), // 19 Hero - 영웅 빨강
        HexC("#FFC93C"), // 20 Honey - 꿀 황금
        HexC("#B19CD9"), // 21 Einstein - 라벤더(천재)
        HexC("#3D7EFF"), // 22 Captain - 캡틴 블루
        HexC("#2DD4BF"), // 23 Orbit - 청록
        HexC("#39FF14"), // 24 Pixel - 네온그린
        HexC("#B19CD9"), // 25 Stardust - 라벤더
        HexC("#5B2C8A"), // 26 Eclipse - 딥퍼플

        // Legendary 27-35 (천체/신화)
        HexC("#FF5252"), // 27 Rudolph - 루돌프 레드
        HexC("#FFF4B8"), // 28 Angel - 천사 백금
        HexC("#FFD24A"), // 29 Apollo - 태양 골드
        HexC("#FFE08A"), // 30 Nova - 백금 폭발
        HexC("#C8D8FF"), // 31 Luna - 은빛 달
        HexC("#FFE830"), // 32 Zeus - 번개 노랑
        HexC("#6A7BFF"), // 33 Titan - 강철 블루
        HexC("#9B30FF"), // 34 Cosmos - 딥 바이올렛
        HexC("#6050E0"), // 35 Galaxy - 푸른 보라
    };

    static Color HexC(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

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
                sprite = (i < catSprites.Length) ? catSprites[i] : null,
                flameColor = (i < FLAME_COLORS.Length) ? FLAME_COLORS[i] : Color.white,
            };
        }
    }

    public CatData Get(int id)
    {
        if (id < 0 || id >= TOTAL_COUNT) return null;
        return catDataCache[id];
    }

    /// <summary>
    /// 고양이별 불꽃 색 가져오기. 안전한 fallback 포함.
    /// </summary>
    public static Color GetFlameColor(int catId)
    {
        if (catId < 0 || catId >= FLAME_COLORS.Length)
            return new Color(1f, 0.5f, 0.1f); // 기본 주황
        return FLAME_COLORS[catId];
    }

    /// <summary>
    /// 평상시 불꽃 startColor MinMax (베이스 색 ± 약간의 밝기 차).
    /// </summary>
    public static ParticleSystem.MinMaxGradient BuildFlameStartColor(Color baseColor, float alpha = 0.8f)
    {
        Color lighter = LightenColor(baseColor, 0.15f);
        Color darker = LightenColor(baseColor, -0.1f);
        lighter.a = alpha;
        darker.a = alpha;
        return new ParticleSystem.MinMaxGradient(darker, lighter);
    }

    /// <summary>
    /// 평상시 불꽃 colorOverLifetime 그라데이션.
    /// 시간 0: 밝게 → 0.5: base → 1.0: 어둡게
    /// </summary>
    public static Gradient BuildFlameGradient(Color baseColor)
    {
        Gradient g = new Gradient();
        Color start = LightenColor(baseColor, 0.2f);
        Color mid = baseColor;
        Color end = LightenColor(baseColor, -0.4f);

        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(mid, 0.5f),
                new GradientColorKey(end, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        return g;
    }

    /// <summary>
    /// RGB 색을 amount만큼 밝게(+) 또는 어둡게(-). amount: -1~+1.
    /// </summary>
    static Color LightenColor(Color c, float amount)
    {
        if (amount >= 0)
        {
            return new Color(
                Mathf.Clamp01(c.r + (1f - c.r) * amount),
                Mathf.Clamp01(c.g + (1f - c.g) * amount),
                Mathf.Clamp01(c.b + (1f - c.b) * amount),
                c.a
            );
        }
        else
        {
            float a = 1f + amount; // 0~1
            return new Color(c.r * a, c.g * a, c.b * a, c.a);
        }
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
