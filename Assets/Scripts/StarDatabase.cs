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
        new StarData { nameKey="star_moon",       typeKey="type_moon",           distanceKM=384400,             color=HexColor("#cccccc"), reward=10 },
        new StarData { nameKey="star_mars",       typeKey="type_terrestrial",    distanceKM=78300000,           color=HexColor("#cc5533"), reward=30 },
        new StarData { nameKey="star_jupiter",    typeKey="type_gas_giant",      distanceKM=628700000,          color=HexColor("#ddaa77"), reward=60 },
        new StarData { nameKey="star_saturn",     typeKey="type_gas_giant",      distanceKM=1275000000,         color=HexColor("#eedd88"), reward=100 },
        new StarData { nameKey="star_uranus",     typeKey="type_ice_giant",      distanceKM=2724000000,         color=HexColor("#88ccdd"), reward=150 },
        new StarData { nameKey="star_neptune",    typeKey="type_ice_giant",      distanceKM=4351000000,         color=HexColor("#4477cc"), reward=220 },
        new StarData { nameKey="star_pluto",      typeKey="type_dwarf_planet",   distanceKM=5906000000,         color=HexColor("#bbaa99"), reward=350 },
        new StarData { nameKey="star_proxima",    typeKey="type_red_dwarf",      distanceKM=40113000000000,     color=HexColor("#ff6644"), reward=800 },
        new StarData { nameKey="star_alpha",      typeKey="type_g_type",         distanceKM=41340000000000,     color=HexColor("#fff4cc"), reward=900 },
        new StarData { nameKey="star_barnard",    typeKey="type_red_dwarf",      distanceKM=56390000000000,     color=HexColor("#ff8866"), reward=1500 },
        new StarData { nameKey="star_sirius",     typeKey="type_binary",         distanceKM=81360000000000,     color=HexColor("#ccddff"), reward=3000 },
        new StarData { nameKey="star_epsilon",    typeKey="type_k_type",         distanceKM=99340000000000,     color=HexColor("#ffcc88"), reward=5000 },
        new StarData { nameKey="star_tau",        typeKey="type_g_type",         distanceKM=112680000000000,    color=HexColor("#fff8dd"), reward=8000 },
        new StarData { nameKey="star_vega",       typeKey="type_a_type",         distanceKM=236900000000000,    color=HexColor("#ddeeff"), reward=20000 },
        new StarData { nameKey="star_arcturus",   typeKey="type_red_giant",      distanceKM=347200000000000,    color=HexColor("#ffaa44"), reward=50000 },
        new StarData { nameKey="star_betelgeuse", typeKey="type_red_supergiant", distanceKM=6620000000000000,   color=HexColor("#ff4422"), reward=200000 },
    };

    public static string FormatKM(double km)
    {
        if (km < 0) km = 0;

        bool isKo = Loc.Get("hud_heading_to").Contains("향하는중");

        if (isKo)
        {
            if (km < 10000) return km.ToString("F0") + " km";
            if (km < 100000000) return (km / 10000).ToString("F0") + "만 km";
            if (km < 1000000000000) return (km / 100000000).ToString("F1") + "억 km";
            if (km < 1000000000000000) return (km / 1000000000000).ToString("F2") + "조 km";
            return (km / 1000000000000000).ToString("F2") + "경 km";
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
    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}