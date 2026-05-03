using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 친구창의 한 행. 좌측 이름/거리/상태, 가운데 고양이, 우측 활동.
/// </summary>
public class FriendCardUI : MonoBehaviour
{
    [Header("Left Column")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI distanceText;
    public Image statusDot;
    public TextMeshProUGUI statusText;

    [Header("Center")]
    public Image catImage;

    [Header("Right Column")]
    public TextMeshProUGUI activityText;

    [Header("Status Colors")]
    public Color sailingColor = new Color(0.36f, 0.79f, 0.36f);  // 초록
    public Color onlineColor = new Color(1f, 0.84f, 0f);         // 노랑
    public Color offlineColor = new Color(0.5f, 0.5f, 0.5f);     // 회색

    public void Bind(FriendEntry e)
    {
        // 이름
        if (nameText != null)
            nameText.text = e.isMe ? "You" : e.displayName;

        // 누적거리 (지구로부터)
        if (distanceText != null)
        {
            string label = Loc.Get("far_from_earth");
            distanceText.text = $"{label}\n{StarDatabase.FormatKM(e.cumulativeKM)}";
        }

        // 상태 (본인은 표시 안 함)
        if (e.isMe)
        {
            if (statusDot != null) statusDot.enabled = false;
            if (statusText != null) statusText.text = "";
        }
        else
        {
            if (statusDot != null) statusDot.enabled = true;
            ApplyStatus(e.status);
        }

        // 고양이 스킨
        if (catImage != null && CatDatabase.Instance != null)
        {
            int safeId = Mathf.Clamp(e.catId, 0, CatDatabase.TOTAL_COUNT - 1);
            CatData cat = CatDatabase.Instance.Get(safeId);
            if (cat != null && cat.sprite != null)
            {
                catImage.sprite = cat.sprite;
                catImage.enabled = true;
                catImage.preserveAspect = true;
            }
            else
            {
                catImage.enabled = false;
            }
        }

        // 우측 활동 표시
        if (activityText != null)
        {
            int safeStarIdx = Mathf.Clamp(e.currentStarIndex, 0, StarDatabase.Stars.Length - 1);
            StarData star = StarDatabase.Stars[safeStarIdx];
            string starName = Loc.Get(star.nameKey);

            // 항해중 + 도착 안 함 → 진행률 표시
            if (e.status == FriendStatus.Sailing && !e.isDocked)
            {
                float pct = (star.distanceKM > 0)
                    ? Mathf.Clamp01((float)(e.distanceKM / star.distanceKM)) * 100f
                    : 0f;
                activityText.text = Loc.Get("friend_heading_to", starName, $"{pct:F0}");
            }
            else
            {
                // 정박 / 온라인 / 오프라인
                activityText.text = Loc.Get("friend_resting_on", starName);
            }
        }
    }

    void ApplyStatus(FriendStatus s)
    {
        Color c;
        string key;
        switch (s)
        {
            case FriendStatus.Sailing: c = sailingColor; key = "status_sailing"; break;
            case FriendStatus.Online:  c = onlineColor;  key = "status_online";  break;
            default:                   c = offlineColor; key = "status_offline"; break;
        }

        if (statusDot != null) statusDot.color = c;
        if (statusText != null)
        {
            statusText.text = Loc.Get(key);
            statusText.color = c;
        }
    }
}
