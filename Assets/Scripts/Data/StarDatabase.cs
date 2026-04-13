using UnityEngine;

[System.Serializable]
public class StarData
{
    public string name;
    public double distanceKM;  // 거리 (km)
    public string starType;
    public Color color;
    public int reward;
    public string description;
}

public static class StarDatabase
{
    public static readonly StarData[] Stars = new StarData[]
    {
        // ========== 태양계 ==========
        new StarData {
            name = "달", distanceKM = 384400,
            starType = "Moon", color = HexColor("#cccccc"),
            reward = 10, description = "지구의 유일한 위성"
        },
        new StarData {
            name = "금성", distanceKM = 41400000,
            starType = "Terrestrial", color = HexColor("#ffddaa"),
            reward = 20, description = "샛별, 가장 뜨거운 행성"
        },
        new StarData {
            name = "화성", distanceKM = 78300000,
            starType = "Terrestrial", color = HexColor("#cc5533"),
            reward = 30, description = "붉은 행성, 인류의 다음 목표"
        },
        new StarData {
            name = "목성", distanceKM = 628700000,
            starType = "Gas Giant", color = HexColor("#ddaa77"),
            reward = 60, description = "태양계 최대 행성"
        },
        new StarData {
            name = "토성", distanceKM = 1275000000,
            starType = "Gas Giant", color = HexColor("#eedd88"),
            reward = 100, description = "아름다운 고리를 가진 행성"
        },
        new StarData {
            name = "천왕성", distanceKM = 2724000000,
            starType = "Ice Giant", color = HexColor("#88ccdd"),
            reward = 150, description = "옆으로 누워 자전하는 행성"
        },
        new StarData {
            name = "해왕성", distanceKM = 4351000000,
            starType = "Ice Giant", color = HexColor("#4477cc"),
            reward = 220, description = "태양계 최외곽 행성"
        },
        new StarData {
            name = "명왕성", distanceKM = 5906000000,
            starType = "Dwarf Planet", color = HexColor("#bbaa99"),
            reward = 350, description = "카이퍼 벨트의 왜소행성"
        },

        // ========== 항성 (1 ly = 9,461,000,000,000 km) ==========
        new StarData {
            name = "Proxima Centauri", distanceKM = 40113000000000,
            starType = "Red Dwarf", color = HexColor("#ff6644"),
            reward = 800, description = "가장 가까운 항성"
        },
        new StarData {
            name = "Alpha Centauri A", distanceKM = 41340000000000,
            starType = "G-type Star", color = HexColor("#fff4cc"),
            reward = 900, description = "태양과 비슷한 쌍성"
        },
        new StarData {
            name = "Barnard's Star", distanceKM = 56390000000000,
            starType = "Red Dwarf", color = HexColor("#ff8866"),
            reward = 1500, description = "가장 빠르게 이동하는 항성"
        },
        new StarData {
            name = "Sirius", distanceKM = 81360000000000,
            starType = "Binary Star", color = HexColor("#ccddff"),
            reward = 3000, description = "밤하늘에서 가장 밝은 별"
        },
        new StarData {
            name = "Epsilon Eridani", distanceKM = 99340000000000,
            starType = "K-type Star", color = HexColor("#ffcc88"),
            reward = 5000, description = "외계행성이 확인된 항성"
        },
        new StarData {
            name = "Tau Ceti", distanceKM = 112680000000000,
            starType = "G-type Star", color = HexColor("#fff8dd"),
            reward = 8000, description = "지구형 행성 후보 보유"
        },
        new StarData {
            name = "Vega", distanceKM = 236900000000000,
            starType = "A-type Star", color = HexColor("#ddeeff"),
            reward = 20000, description = "직녀성, 거문고자리"
        },
        new StarData {
            name = "Arcturus", distanceKM = 347200000000000,
            starType = "Red Giant", color = HexColor("#ffaa44"),
            reward = 50000, description = "봄철 대삼각형의 주인공"
        },
        new StarData {
            name = "Betelgeuse", distanceKM = 6620000000000000,
            starType = "Red Supergiant", color = HexColor("#ff4422"),
            reward = 200000, description = "곧 초신성 폭발 예정"
        },
    };

    /// <summary>
    /// km을 읽기 쉽게 자동 변환.
    /// </summary>
    public static string FormatKM(double km)
    {
        if (km < 0) km = 0;
        if (km < 10000)
            return km.ToString("F0") + " km";
        if (km < 100000000)
            return (km / 10000).ToString("F0") + "만 km";
        if (km < 1000000000000)
            return (km / 100000000).ToString("F1") + "억 km";
        if (km < 1000000000000000)
            return (km / 1000000000000).ToString("F2") + "조 km";
        return (km / 1000000000000000).ToString("F2") + "경 km";
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
