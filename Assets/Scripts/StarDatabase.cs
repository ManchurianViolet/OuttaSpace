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
    public static readonly StarData[] Stars = new StarData[]
    {
        // ===== 태양계 (0~6) =====
        new StarData { nameKey="star_moon",       typeKey="type_moon",            distanceKM=384400,            color=HexColor("#cccccc"), reward=10 },
        new StarData { nameKey="star_mars",       typeKey="type_terrestrial",     distanceKM=77915600,          color=HexColor("#cc5533"), reward=30 },
        new StarData { nameKey="star_jupiter",    typeKey="type_gas_giant",       distanceKM=550415600,         color=HexColor("#ddaa77"), reward=60 },
        new StarData { nameKey="star_saturn",     typeKey="type_gas_giant",       distanceKM=646300000,         color=HexColor("#eedd88"), reward=100 },
        new StarData { nameKey="star_uranus",     typeKey="type_ice_giant",       distanceKM=1449000000,        color=HexColor("#88ccdd"), reward=150 },
        new StarData { nameKey="star_neptune",    typeKey="type_ice_giant",       distanceKM=1627000000,        color=HexColor("#4477cc"), reward=220 },
        new StarData { nameKey="star_pluto",      typeKey="type_dwarf_planet",    distanceKM=1555000000,        color=HexColor("#bbaa99"), reward=350 },

        // ===== 근접 항성 (7~19) =====
        new StarData { nameKey="star_proxima",    typeKey="type_red_dwarf",       distanceKM=39694093984400,    color=HexColor("#ff6644"), reward=800 },
        new StarData { nameKey="star_alpha",      typeKey="type_g_type",          distanceKM=1600000000000,     color=HexColor("#fff4cc"), reward=900 },
        new StarData { nameKey="star_barnard",    typeKey="type_red_dwarf",       distanceKM=15100000000000,    color=HexColor("#ff8866"), reward=1500 },
        new StarData { nameKey="star_wolf359",    typeKey="type_red_dwarf",       distanceKM=18000000000000,    color=HexColor("#ff7755"), reward=2200 },
        new StarData { nameKey="star_lalande",    typeKey="type_red_dwarf",       distanceKM=4100000000000,     color=HexColor("#ff8866"), reward=2500 },
        new StarData { nameKey="star_sirius",     typeKey="type_white_binary",    distanceKM=2900000000000,     color=HexColor("#ccddff"), reward=3000 },
        new StarData { nameKey="star_luyten726",  typeKey="type_red_binary",      distanceKM=1200000000000,     color=HexColor("#ff8866"), reward=3200 },
        new StarData { nameKey="star_ross154",    typeKey="type_red_dwarf",       distanceKM=9100000000000,     color=HexColor("#ff7755"), reward=3500 },
        new StarData { nameKey="star_epsilon",    typeKey="type_k_type",          distanceKM=7600000000000,     color=HexColor("#ffcc88"), reward=4000 },
        new StarData { nameKey="star_ross128",    typeKey="type_red_dwarf",       distanceKM=4700000000000,     color=HexColor("#ff7755"), reward=4500 },
        new StarData { nameKey="star_procyon",    typeKey="type_f_binary",        distanceKM=4000000000000,     color=HexColor("#fff8e0"), reward=5000 },
        new StarData { nameKey="star_cygnus61",   typeKey="type_k_binary",        distanceKM=100000000000,      color=HexColor("#ffcc88"), reward=5500 },
        new StarData { nameKey="star_tau",        typeKey="type_g_type",          distanceKM=5000000000000,     color=HexColor("#fff8dd"), reward=6000 },

        // ===== 밝은 별/거성 (20~38) =====
        new StarData { nameKey="star_altair",     typeKey="type_a_type",          distanceKM=38000000000000,    color=HexColor("#e0eeff"), reward=8000 },
        new StarData { nameKey="star_vega",       typeKey="type_a_type",          distanceKM=86000000000000,    color=HexColor("#ddeeff"), reward=12000 },
        new StarData { nameKey="star_fomalhaut",  typeKey="type_a_type",          distanceKM=100000000000,      color=HexColor("#e8f0ff"), reward=13000 },
        new StarData { nameKey="star_pollux",     typeKey="type_orange_giant",    distanceKM=83000000000000,    color=HexColor("#ffaa66"), reward=16000 },
        new StarData { nameKey="star_denebola",   typeKey="type_a_type",          distanceKM=21000000000000,    color=HexColor("#ddeeff"), reward=18000 },
        new StarData { nameKey="star_arcturus",   typeKey="type_red_giant",       distanceKM=9000000000000,     color=HexColor("#ffaa44"), reward=20000 },
        new StarData { nameKey="star_capella",    typeKey="type_yellow_giant",    distanceKM=47000000000000,    color=HexColor("#ffe680"), reward=25000 },
        new StarData { nameKey="star_castor",     typeKey="type_white_multi",     distanceKM=86000000000000,    color=HexColor("#e8f0ff"), reward=30000 },
        new StarData { nameKey="star_aldebaran",  typeKey="type_red_giant",       distanceKM=132000000000000,   color=HexColor("#ff9933"), reward=40000 },
        new StarData { nameKey="star_hamal",      typeKey="type_orange_giant",    distanceKM=8000000000000,     color=HexColor("#ffaa66"), reward=42000 },
        new StarData { nameKey="star_regulus",    typeKey="type_white_multi",     distanceKM=110000000000000,   color=HexColor("#e8f0ff"), reward=50000 },
        new StarData { nameKey="star_mizar",      typeKey="type_white_multi",     distanceKM=51000000000000,    color=HexColor("#e8f0ff"), reward=55000 },
        new StarData { nameKey="star_algol",      typeKey="type_eclipsing",       distanceKM=67000000000000,    color=HexColor("#ddeeff"), reward=60000 },
        new StarData { nameKey="star_alkaid",     typeKey="type_a_type",          distanceKM=133000000000000,   color=HexColor("#bbccff"), reward=70000 },
        new StarData { nameKey="star_achernar",   typeKey="type_b_type",          distanceKM=336000000000000,   color=HexColor("#aaccff"), reward=90000 },
        new StarData { nameKey="star_hyades",     typeKey="type_open_cluster",    distanceKM=130000000000000,   color=HexColor("#ffdd99"), reward=95000 },
        new StarData { nameKey="star_alphard",    typeKey="type_orange_giant",    distanceKM=220000000000000,   color=HexColor("#ffaa66"), reward=110000 },
        new StarData { nameKey="star_bellatrix",  typeKey="type_b_type",          distanceKM=650000000000000,   color=HexColor("#aaccff"), reward=130000 },
        new StarData { nameKey="star_spica",      typeKey="type_b_type",          distanceKM=50000000000000,    color=HexColor("#aaccff"), reward=135000 },

        // ===== 변광성/초거성/특이천체 (39~58) =====
        new StarData { nameKey="star_mira",       typeKey="type_variable",        distanceKM=470000000000000,   color=HexColor("#ff5544"), reward=150000 },
        new StarData { nameKey="star_canopus",    typeKey="type_white_supergiant",distanceKM=90000000000000,    color=HexColor("#fff8e8"), reward=160000 },
        new StarData { nameKey="star_rxj1856",    typeKey="type_neutron_star",    distanceKM=850000000000000,   color=HexColor("#aabbff"), reward=180000 },
        new StarData { nameKey="star_pleiades",   typeKey="type_open_cluster",    distanceKM=420000000000000,   color=HexColor("#aaccff"), reward=200000 },
        new StarData { nameKey="star_polaris",    typeKey="type_yellow_supergiant",distanceKM=30000000000000,   color=HexColor("#ffefb0"), reward=210000 },
        new StarData { nameKey="star_mirfak",     typeKey="type_yellow_supergiant",distanceKM=600000000000000,  color=HexColor("#fff0c0"), reward=230000 },
        new StarData { nameKey="star_betelgeuse", typeKey="type_red_supergiant",  distanceKM=110000000000000,   color=HexColor("#ff4422"), reward=250000 },
        new StarData { nameKey="star_gamcas",     typeKey="type_be_star",         distanceKM=260000000000000,   color=HexColor("#bbddff"), reward=260000 },
        new StarData { nameKey="star_antares",    typeKey="type_red_supergiant",  distanceKM=40000000000000,    color=HexColor("#ff5533"), reward=280000 },
        new StarData { nameKey="star_beehive",    typeKey="type_open_cluster",    distanceKM=530000000000000,   color=HexColor("#ffeedd"), reward=300000 },
        new StarData { nameKey="star_helix",      typeKey="type_planetary_nebula",distanceKM=430000000000000,   color=HexColor("#88ddff"), reward=320000 },
        new StarData { nameKey="star_rigel",      typeKey="type_blue_supergiant", distanceKM=1110000000000000,  color=HexColor("#aaccff"), reward=360000 },
        new StarData { nameKey="star_vela",       typeKey="type_pulsar",          distanceKM=1760000000000000,  color=HexColor("#ddeeff"), reward=400000 },
        new StarData { nameKey="star_orion_neb",  typeKey="type_emission_nebula", distanceKM=3630000000000000,  color=HexColor("#ff88aa"), reward=450000 },
        new StarData { nameKey="star_dumbbell",   typeKey="type_planetary_nebula",distanceKM=200000000000000,   color=HexColor("#88ffcc"), reward=480000 },
        new StarData { nameKey="star_gaiabh1",    typeKey="type_black_hole",      distanceKM=1900000000000000,  color=HexColor("#221133"), reward=500000 },
        new StarData { nameKey="star_wezen",      typeKey="type_yellow_supergiant",distanceKM=300000000000000,  color=HexColor("#fff0c0"), reward=520000 },
        new StarData { nameKey="star_ring",       typeKey="type_planetary_nebula",distanceKM=6500000000000000,  color=HexColor("#88ccff"), reward=600000 },
        new StarData { nameKey="star_deneb",      typeKey="type_blue_supergiant", distanceKM=3000000000000000,  color=HexColor("#bbddff"), reward=700000 },

        // ===== 성간 영역 (59~85) =====
        new StarData { nameKey="star_mucep",      typeKey="type_red_supergiant",  distanceKM=2300000000000000,  color=HexColor("#cc3322"), reward=800000 },
        new StarData { nameKey="star_catseye",    typeKey="type_planetary_nebula",distanceKM=4000000000000000,  color=HexColor("#cc88ff"), reward=900000 },
        new StarData { nameKey="star_gaiabh2",    typeKey="type_black_hole",      distanceKM=5100000000000000,  color=HexColor("#221133"), reward=1100000 },
        new StarData { nameKey="star_vycma",      typeKey="type_hypergiant",      distanceKM=100000000000000,   color=HexColor("#ff3311"), reward=1200000 },
        new StarData { nameKey="star_lagoon",     typeKey="type_emission_nebula", distanceKM=2500000000000000,  color=HexColor("#ff77aa"), reward=1400000 },
        new StarData { nameKey="star_trifid",     typeKey="type_emission_nebula", distanceKM=200000000000000,   color=HexColor("#ff66bb"), reward=1500000 },
        new StarData { nameKey="star_nmlcyg",     typeKey="type_hypergiant",      distanceKM=11300000000000000, color=HexColor("#ff2211"), reward=1800000 },
        new StarData { nameKey="star_eagle",      typeKey="type_emission_nebula", distanceKM=3800000000000000,  color=HexColor("#ff8899"), reward=2000000 },
        new StarData { nameKey="star_uyscuti",    typeKey="type_red_supergiant",  distanceKM=1900000000000000,  color=HexColor("#cc3322"), reward=2200000 },
        new StarData { nameKey="star_wildduck",   typeKey="type_open_cluster",    distanceKM=2900000000000000,  color=HexColor("#ffeedd"), reward=2400000 },
        new StarData { nameKey="star_crab",       typeKey="type_supernova_remnant",distanceKM=2800000000000000, color=HexColor("#aa66ff"), reward=2600000 },
        new StarData { nameKey="star_cygx1",      typeKey="type_black_hole",      distanceKM=6600000000000000,  color=HexColor("#221133"), reward=2900000 },
        new StarData { nameKey="star_carina",     typeKey="type_emission_nebula", distanceKM=2900000000000000,  color=HexColor("#ff7788"), reward=3100000 },
        new StarData { nameKey="star_etacar",     typeKey="type_lbv",             distanceKM=100000000000000,   color=HexColor("#ff5500"), reward=3200000 },
        new StarData { nameKey="star_doublec",    typeKey="type_open_cluster",    distanceKM=100000000000000,   color=HexColor("#ffeedd"), reward=3300000 },
        new StarData { nameKey="star_v404",       typeKey="type_black_hole",      distanceKM=2800000000000000,  color=HexColor("#221133"), reward=3500000 },
        new StarData { nameKey="star_rhocas",     typeKey="type_yellow_hypergiant",distanceKM=3800000000000000, color=HexColor("#ffdd44"), reward=3700000 },
        new StarData { nameKey="star_wr104",      typeKey="type_wolf_rayet",      distanceKM=1900000000000000,  color=HexColor("#ffaa44"), reward=3900000 },
        new StarData { nameKey="star_west1_26",   typeKey="type_hypergiant",      distanceKM=43500000000000000, color=HexColor("#ff2200"), reward=5000000 },
        new StarData { nameKey="star_47tuc",      typeKey="type_globular_cluster",distanceKM=14000000000000000, color=HexColor("#ffeecc"), reward=5500000 },
        new StarData { nameKey="star_omegacen",   typeKey="type_globular_cluster",distanceKM=24000000000000000, color=HexColor("#ffeecc"), reward=6500000 },
        new StarData { nameKey="star_stephenson", typeKey="type_hypergiant",      distanceKM=18000000000000000, color=HexColor("#cc1100"), reward=7500000 },
        new StarData { nameKey="star_m13",        typeKey="type_globular_cluster",distanceKM=31000000000000000, color=HexColor("#ffeecc"), reward=8500000 },
        new StarData { nameKey="star_pistol",     typeKey="type_lbv",             distanceKM=27000000000000000, color=HexColor("#ff6600"), reward=9500000 },
        new StarData { nameKey="star_sgra",       typeKey="type_smbh",            distanceKM=9000000000000000,  color=HexColor("#110022"), reward=11000000 },
        new StarData { nameKey="star_m15",        typeKey="type_globular_cluster",distanceKM=72000000000000000, color=HexColor("#ffeecc"), reward=13000000 },
        new StarData { nameKey="star_m3",         typeKey="type_globular_cluster",distanceKM=3000000000000000,  color=HexColor("#ffeecc"), reward=13500000 },

        // ===== 은하간 영역 (86~99) =====
        new StarData { nameKey="star_tarantula",  typeKey="type_emission_nebula", distanceKM=1.189e+18,         color=HexColor("#ff5588"), reward=30000000 },
        new StarData { nameKey="star_lmc",        typeKey="type_galaxy",          distanceKM=30000000000000000, color=HexColor("#ffccaa"), reward=35000000 },
        new StarData { nameKey="star_wohg64",     typeKey="type_hypergiant",      distanceKM=1000000000000000,  color=HexColor("#aa0000"), reward=36000000 },
        new StarData { nameKey="star_smc",        typeKey="type_galaxy",          distanceKM=3.5e+17,           color=HexColor("#ffccaa"), reward=45000000 },
        new StarData { nameKey="star_andromeda",  typeKey="type_spiral_galaxy",   distanceKM=2.211e+19,         color=HexColor("#ffeebb"), reward=100000000 },
        new StarData { nameKey="star_triangulum", typeKey="type_spiral_galaxy",   distanceKM=1.8e+18,           color=HexColor("#ffeebb"), reward=120000000 },
        new StarData { nameKey="star_sculptor",   typeKey="type_spiral_galaxy",   distanceKM=8.22e+19,          color=HexColor("#ffe8aa"), reward=500000000 },
        new StarData { nameKey="star_m82",        typeKey="type_starburst_galaxy",distanceKM=1e+18,             color=HexColor("#ffaa66"), reward=550000000 },
        new StarData { nameKey="star_m81",        typeKey="type_spiral_galaxy",   distanceKM=3e+18,             color=HexColor("#ffeebb"), reward=600000000 },
        new StarData { nameKey="star_centaurusA", typeKey="type_elliptical_galaxy",distanceKM=1.1e+19,          color=HexColor("#ffccaa"), reward=700000000 },
        new StarData { nameKey="star_blackeye",   typeKey="type_spiral_galaxy",   distanceKM=3.8e+19,           color=HexColor("#ddaa88"), reward=900000000 },
        new StarData { nameKey="star_pinwheel",   typeKey="type_spiral_galaxy",   distanceKM=3.6e+19,           color=HexColor("#ffeebb"), reward=1200000000 },
        new StarData { nameKey="star_whirlpool",  typeKey="type_spiral_galaxy",   distanceKM=2.2e+19,           color=HexColor("#ffeebb"), reward=1400000000 },
        new StarData { nameKey="star_sombrero",   typeKey="type_lenticular_galaxy",distanceKM=5.9e+19,          color=HexColor("#ffe8bb"), reward=1800000000 },
        new StarData { nameKey="star_m87",        typeKey="type_elliptical_galaxy",distanceKM=2.28e+20,         color=HexColor("#ffccaa"), reward=3000000000 },
    };

    public static string FormatKM(double km)
    {
        if (km < 0) km = 0;

        var locale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
        string lang = (locale != null) ? locale.Identifier.Code : "en";
        bool isAsianUnits = (lang == "ko" || lang == "ja");

        if (isAsianUnits)
        {
            // 한/일 공유: 万/億/兆/京/垓 (한국어는 한글, 일본어는 한자)
            string manUnit  = (lang == "ja") ? "万" : "만";
            string okUnit   = (lang == "ja") ? "億" : "억";
            string joUnit   = (lang == "ja") ? "兆" : "조";
            string gyeong   = (lang == "ja") ? "京" : "경";
            string hae      = (lang == "ja") ? "垓" : "해";

            if (km < 10000)                    return km.ToString("F0") + " km";
            if (km < 100000000)                return (km / 10000).ToString("F0") + manUnit + " km";
            if (km < 1000000000000)            return (km / 100000000).ToString("F1") + okUnit + " km";
            if (km < 10000000000000000)        return (km / 1000000000000).ToString("F2") + joUnit + " km";
            if (km < 100000000000000000000.0)  return (km / 10000000000000000.0).ToString("F2") + gyeong + " km";
            return (km / 100000000000000000000.0).ToString("F2") + hae + " km";
        }
        else
        {
            // 영/중: K/M/B/T/Q (Quadrillion)/Qt (Quintillion)
            if (km < 1000)                  return km.ToString("F0") + " km";
            if (km < 1000000)               return (km / 1000).ToString("F1") + "K km";
            if (km < 1000000000)            return (km / 1000000).ToString("F1") + "M km";
            if (km < 1000000000000)         return (km / 1000000000).ToString("F1") + "B km";
            if (km < 1000000000000000)      return (km / 1000000000000).ToString("F2") + "T km";
            if (km < 1000000000000000000.0) return (km / 1000000000000000.0).ToString("F2") + "Qa km";
            return (km / 1000000000000000000.0).ToString("F2") + "Qi km";
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
