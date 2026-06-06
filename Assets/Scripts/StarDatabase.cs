using UnityEngine;

[System.Serializable]
public class StarData
{
    public string nameKey;
    public string typeKey;
    public double distanceKM;
    public Color color;
    public int reward;
}

public static class StarDatabase
{
    /// <summary>
    /// 데모 빌드에서 마지막으로 도달 가능한 별 index.
    /// 11 = 시리우스 (Sirius). 도착하면 출발 버튼 비활성 + "데모 끝" 안내.
    /// 정식 출시 시 -1로 바꾸면 자동으로 끝까지 갈 수 있음.
    /// </summary>
    public const int DEMO_LAST_STAR_INDEX = 29;

    /// <summary>
    /// 데모 잠금 대상인지 (data 없음 + 도착 불가).
    /// DEMO_LAST_STAR_INDEX 이후 별이면 true.
    /// </summary>
    public static bool IsDemoLocked(int starIndex)
    {
        if (DEMO_LAST_STAR_INDEX < 0) return false;
        return starIndex > DEMO_LAST_STAR_INDEX;
    }

    public static readonly StarData[] Stars = new StarData[]
    {
        // === 태양계 (7개) ===
        new StarData { nameKey="star_moon",       typeKey="type_moon",            distanceKM=384400,             color=HexColor("#cccccc"), reward=10 },
        new StarData { nameKey="star_mars",       typeKey="type_terrestrial",     distanceKM=77915600,           color=HexColor("#cc5533"), reward=30 },
        new StarData { nameKey="star_jupiter",    typeKey="type_gas_giant",       distanceKM=550400000,          color=HexColor("#ddaa77"), reward=60 },
        new StarData { nameKey="star_saturn",     typeKey="type_gas_giant",       distanceKM=646300000,         color=HexColor("#eedd88"), reward=100 },
        new StarData { nameKey="star_uranus",     typeKey="type_ice_giant",       distanceKM=1449000000,         color=HexColor("#88ccdd"), reward=150 },
        new StarData { nameKey="star_neptune",    typeKey="type_ice_giant",       distanceKM=1627000000,         color=HexColor("#4477cc"), reward=220 },
        new StarData { nameKey="star_pluto",      typeKey="type_dwarf_planet",    distanceKM=1555000000,         color=HexColor("#bbaa99"), reward=350 },

        // === 근접 항성 (8~18) ===
        new StarData { nameKey="star_proxima",    typeKey="type_red_dwarf",       distanceKM=40165500000000,     color=HexColor("#ff6644"), reward=800 },
        new StarData { nameKey="star_alpha",      typeKey="type_g_type",          distanceKM=1144781000000,     color=HexColor("#fff4cc"), reward=900 },
        new StarData { nameKey="star_barnard",    typeKey="type_red_dwarf",       distanceKM=15071373000000,     color=HexColor("#ff8866"), reward=1500 },
        new StarData { nameKey="star_wolf359",    typeKey="type_red_dwarf",       distanceKM=17975900000000,     color=HexColor("#ff7755"), reward=2200 },
        new StarData { nameKey="star_sirius",     typeKey="type_white_binary",    distanceKM=7001140000000,     color=HexColor("#ccddff"), reward=3000 },
        new StarData { nameKey="star_luyten",     typeKey="type_red_binary",      distanceKM=1229930000000,     color=HexColor("#ff8866"), reward=3500 },
        new StarData { nameKey="star_epsilon",    typeKey="type_k_type",          distanceKM=16745970000000,     color=HexColor("#ffcc88"), reward=5000 },
        new StarData { nameKey="star_ross128",    typeKey="type_red_dwarf",       distanceKM=5014330000000,    color=HexColor("#ff7755"), reward=5800 },
        new StarData { nameKey="star_procyon",    typeKey="type_f_binary",        distanceKM=3595180000000,    color=HexColor("#fff8e0"), reward=6500 },
        new StarData { nameKey="star_tau",        typeKey="type_g_type",          distanceKM=4730500000000,    color=HexColor("#fff8dd"), reward=8000 },
        new StarData { nameKey="star_cygnus61",   typeKey="type_k_binary",        distanceKM=4730500000000,    color=HexColor("#ffcc88"), reward=9000 },

        // === 밝은 별 (19~25) ===
        new StarData { nameKey="star_altair",     typeKey="type_a_type",          distanceKM=50332520000000,    color=HexColor("#e0eeff"), reward=14000 },
        new StarData { nameKey="star_vega",       typeKey="type_a_type",          distanceKM=78620910000000,    color=HexColor("#ddeeff"), reward=20000 },
        new StarData { nameKey="star_fomalhaut",  typeKey="type_a_type",          distanceKM=851490000000,    color=HexColor("#e8f0ff"), reward=22000 },
        new StarData { nameKey="star_denebola",   typeKey="type_a_type",          distanceKM=101894970000000,    color=HexColor("#ddeeff"), reward=40000 },
        new StarData { nameKey="star_arcturus",   typeKey="type_red_giant",      distanceKM=7568800000000,    color=HexColor("#ffaa44"), reward=50000 },
        new StarData { nameKey="star_capella",    typeKey="type_yellow_giant",    distanceKM=58847420000000,    color=HexColor("#ffe680"), reward=70000 },
        new StarData { nameKey="star_aldebaran",  typeKey="type_red_giant",       distanceKM=211074910000000,    color=HexColor("#ff9933"), reward=110000 },

        // === 초거성 (26~30) ===
        new StarData { nameKey="star_polaris",    typeKey="type_yellow_supergiant", distanceKM=3479471970000000,  color=HexColor("#ffefb0"), reward=150000 },
        new StarData { nameKey="star_antares",    typeKey="type_red_supergiant",  distanceKM=1106937000000000,   color=HexColor("#ff5533"), reward=180000 },
        new StarData { nameKey="star_betelgeuse", typeKey="type_red_supergiant",  distanceKM=1419150000000000,   color=HexColor("#ff4422"), reward=220000 },
        new StarData { nameKey="star_rigel",      typeKey="type_blue_supergiant", distanceKM=1513760000000000,   color=HexColor("#aaccff"), reward=280000 },
        new StarData { nameKey="star_deneb",      typeKey="type_blue_supergiant", distanceKM=16462140000000000,  color=HexColor("#bbddff"), reward=500000 },
    };

    public static string FormatKM(double km)
    {
        if (km < 0) km = 0;

        var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
        string lang = (locale != null) ? locale.Identifier.Code : "en";
        bool isAsianUnits = (lang == "ko" || lang == "ja");

        if (isAsianUnits)
        {
            // 한/일 공유: 万/億/兆/京 (한국어는 한글, 일본어는 한자)
            string manUnit  = (lang == "ja") ? "万" : "만";
            string okUnit   = (lang == "ja") ? "億" : "억";
            string joUnit   = (lang == "ja") ? "兆" : "조";
            string gyeong   = (lang == "ja") ? "京" : "경";

            if (km < 10000) return km.ToString("F0") + " km";
            if (km < 100000000) return (km / 10000).ToString("F0") + manUnit + " km";
            if (km < 1000000000000) return (km / 100000000).ToString("F1") + okUnit + " km";
            if (km < 1000000000000000) return (km / 1000000000000).ToString("F2") + joUnit + " km";
            return (km / 1000000000000000).ToString("F2") + gyeong + " km";
        }
        else
        {
            if (km < 1000) return km.ToString("F0") + " km";
            if (km < 1000000) return (km / 1000).ToString("F1") + "K km";
            if (km < 1000000000) return (km / 1000000).ToString("F1") + "M km";
            if (km < 1000000000000) return (km / 1000000000).ToString("F1") + "B km";
            return (km / 1000000000000).ToString("F2") + "T km";
        }
    }

    /// <summary>
    /// 지구(시작점)부터의 누적 거리.
    /// starIndex = 현재 향하는/도착한 별의 index, currentDistance = 그 별까지의 진행거리
    /// 친구창에서 거리순 정렬 + "지구로부터" 표시에 사용.
    /// </summary>
    public static double GetCumulativeDistance(int starIndex, double currentDistance)
    {
        double sum = 0;
        for (int i = 0; i < starIndex && i < Stars.Length; i++)
            sum += Stars[i].distanceKM;
        return sum + currentDistance;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
