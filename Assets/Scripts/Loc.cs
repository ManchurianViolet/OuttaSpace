using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public static class Loc
{
    public static string tableName = "GameTable";

    private static readonly Dictionary<string, string> fallback = new Dictionary<string, string>
    {
        ["loading_location"] = "현재위치 확인중...",
        ["hud_heading_to"] = "{0}(으)로 향하는중",
        ["hud_arrived_at"] = "{0}에 도착!",
        ["dock_next"] = "다음: {0} ({1})",
        ["dock_eta"] = "예상: {0}",
        ["dock_depart"] = "{0}(으)로 출발 ▶",
        ["dock_last_dest"] = "마지막 목적지입니다",
        ["dock_complete"] = "탐사 완료",
        ["upgrade_speed"] = "추진력",
        ["upgrade_speed_desc"] = "기본 이동 속도 증가",
        ["upgrade_bcap"] = "부스터 용량",
        ["upgrade_bcap_desc"] = "부스터 최대 충전량 증가",
        ["upgrade_bspd"] = "부스터 속도",
        ["upgrade_bspd_desc"] = "부스터 발동 시 배율 증가",
        ["upgrade_cat"] = "고양이 교체",
        ["upgrade_cat_desc"] = "데모에서는 지원하지 않습니다",
        ["star_moon"] = "달",
        ["star_venus"] = "금성",
        ["star_mars"] = "화성",
        ["star_jupiter"] = "목성",
        ["star_saturn"] = "토성",
        ["star_uranus"] = "천왕성",
        ["star_neptune"] = "해왕성",
        ["star_pluto"] = "명왕성",
        ["star_proxima"] = "프록시마 센타우리",
        ["star_alpha"] = "알파 센타우리 A",
        ["star_barnard"] = "바너드 별",
        ["star_sirius"] = "시리우스",
        ["star_epsilon"] = "엡실론 에리다니",
        ["star_tau"] = "타우 세티",
        ["star_vega"] = "베가",
        ["star_arcturus"] = "아크투루스",
        ["star_betelgeuse"] = "베텔게우스",
        ["type_moon"] = "위성",
        ["type_terrestrial"] = "암석형 행성",
        ["type_gas_giant"] = "가스 거대 행성",
        ["type_ice_giant"] = "얼음 거대 행성",
        ["type_dwarf_planet"] = "왜소행성",
        ["type_red_dwarf"] = "적색왜성",
        ["type_g_type"] = "G형 항성",
        ["type_k_type"] = "K형 항성",
        ["type_a_type"] = "A형 항성",
        ["type_binary"] = "쌍성",
        ["type_red_giant"] = "적색거성",
        ["type_red_supergiant"] = "적색초거성",
        ["notif_arrived"] = "★ {0} 도착!",
        ["time_sec"] = "{0}초",
        ["time_min"] = "{0}분",
        ["time_hour"] = "{0}시간",
        ["time_day"] = "{0}일",
        ["speed_kms"] = "{0} km/s",
        ["speed_tkms"] = "{0}천 km/s",
        ["speed_pctc"] = "{0}% c",
        ["speed_c"] = "{0}c",
    };

    public static string Get(string key)
    {
        if (LocalizationSettings.InitializationOperation.IsDone)
        {
            string result = LocalizationSettings.StringDatabase
                .GetLocalizedString(tableName, key);

            if (!string.IsNullOrEmpty(result) && !result.Contains("No translation"))
                return result;
        }

        if (fallback.ContainsKey(key))
            return fallback[key];

        return key;
    }

    public static string Get(string key, params object[] args)
    {
        string template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }
}