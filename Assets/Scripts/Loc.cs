using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public static class Loc
{
    public static string tableName = "GameTable";

    private static readonly Dictionary<string, string> fallback = BuildFallback();

    static Dictionary<string, string> BuildFallback()
    {
        var dict = new Dictionary<string, string>
        {
            ["loading_location"] = "현재위치 확인중...",
            ["hud_heading_to"] = "{0}(으)로 향하는중",
            ["hud_arrived_at"] = "{0}에 도착!",
            ["dock_next"] = "다음: {0} ({1})",
            ["dock_eta"] = "예상: {0}",
            ["dock_depart"] = "출발",
            ["dock_last_dest"] = "마지막 목적지입니다",
            ["dock_complete"] = "탐사 완료",
            ["upgrade_speed"] = "추진력",
            ["upgrade_speed_desc"] = "기본 속도",
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
            ["notif_arrived"] = "{0} 도착!",
            ["time_sec"] = "{0}초",
            ["time_min"] = "{0}분",
            ["time_hour"] = "{0}시간",
            ["time_day"] = "{0}일",
            ["speed_kms"] = "{0} km/s",
            ["speed_tkms"] = "{0}천 km/s",
            ["speed_pctc"] = "{0}% c",
            ["speed_c"] = "{0}c",
            ["rarity_common"] = "일반",
            ["rarity_rare"] = "희귀",
            ["rarity_legendary"] = "전설",
            ["gacha_pull_cost"] = "뽑기 (-{0} CR)",

            // 친구창
            ["status_sailing"] = "항해중",
            ["status_online"] = "온라인",
            ["status_offline"] = "오프라인",
            ["friend_heading_to"] = "{0}(으)로 가는 중 ({1}%)",
            ["friend_resting_on"] = "{0}에 정박 중",
            ["far_from_earth"] = "지구로부터",

            // 설정
            ["settings_language"] = "언어",
            ["settings_size"] = "크기",
            ["settings_credits"] = "크레딧",
            ["settings_language_title"] = "언어 설정",
            ["settings_size_title"] = "크기 설정",
            ["settings_credits_title"] = "크레딧",
            ["settings_widget_label"] = "항해 모드 크기",
            ["settings_station_label"] = "기능 모드 크기",
        };

        // 고양이 이름 자동 생성
        for (int i = 1; i <= 27; i++)
            dict[$"cat_common_{i:D2}"] = $"커먼 {i}";
        for (int i = 1; i <= 9; i++)
            dict[$"cat_rare_{i:D2}"] = $"레어 {i}";
        for (int i = 1; i <= 3; i++)
            dict[$"cat_legendary_{i:D2}"] = $"레전더리 {i}";

        return dict;
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (LocalizationSettings.InitializationOperation.IsDone)
        {
            try
            {
                var table = LocalizationSettings.StringDatabase.GetTable(tableName);
                if (table != null)
                {
                    var entry = table.GetEntry(key);
                    if (entry != null)
                    {
                        string val = entry.GetLocalizedString();
                        if (!string.IsNullOrEmpty(val))
                            return val;
                    }
                }
            }
            catch { }
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
