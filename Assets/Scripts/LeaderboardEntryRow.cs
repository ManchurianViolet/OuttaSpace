using TMPro;
using UnityEngine;

/// <summary>
/// 리더보드 entry 프리팹에 붙이는 컴포넌트.
/// 각 TMP를 인스펙터에서 드래그로 연결하면 LeaderboardUI가 이걸 보고 값을 채움.
/// fromEarthText는 선택사항 (비워두면 거리 표시 생략).
/// </summary>
public class LeaderboardEntryRow : MonoBehaviour
{
    [Tooltip("순위 (#1)")]
    public TextMeshProUGUI rankText;
    [Tooltip("스팀 닉네임")]
    public TextMeshProUGUI nameText;
    [Tooltip("지나간 항성 수 (★ 5)")]
    public TextMeshProUGUI starsText;
    [Tooltip("지구로부터 거리 (≈4.2 × 10^17 km). 선택사항.")]
    public TextMeshProUGUI fromEarthText;
}
