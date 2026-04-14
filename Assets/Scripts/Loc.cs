using UnityEngine.Localization.Settings;

/// <summary>
/// Unity Localization 패키지 래퍼.
/// 모든 스크립트에서 Loc.Get("key")로 호출 — 내부는 Unity String Table 사용.
/// 
/// [사전 설정]
/// 1. Package Manager에서 Localization 패키지 설치
/// 2. Window > Asset Management > Localization Tables
/// 3. String Table Collection "GameTable" 생성
/// 4. Locale 추가: Korean (ko), English (en)
/// 5. CSV 임포트 또는 수동으로 키-값 입력
/// 6. Project Settings > Localization > Default Locale 설정
/// </summary>
public static class Loc
{
    public static string tableName = "GameTable";

    /// <summary>
    /// 키로 로컬라이즈된 문자열 가져오기.
    /// </summary>
    public static string Get(string key)
    {
        var entry = LocalizationSettings.StringDatabase
            .GetLocalizedString(tableName, key);

        // 테이블에 없으면 키 자체를 반환 (디버깅용)
        if (string.IsNullOrEmpty(entry))
            return key;

        return entry;
    }

    /// <summary>
    /// string.Format 포함 버전. Loc.Get("key", arg1, arg2)
    /// </summary>
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