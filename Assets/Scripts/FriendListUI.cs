using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public enum FriendStatus { Sailing, Online, Offline }

public class FriendEntry
{
    public bool isMe;
    public ulong steamId;
    public string displayName;
    public int catId;
    public int currentStarIndex;
    public double distanceKM;
    public bool isDocked;
    public FriendStatus status;
    public double cumulativeKM;
}

/// <summary>
/// 친구창 메인. 본인 + 캐시된 친구들 + 실시간 데이터 합쳐서 거리순으로 표시.
/// </summary>
public class FriendListUI : MonoBehaviour
{
    [Header("References")]
    public Transform cardContainer;       // ScrollRect의 Content (Vertical Layout Group)
    public FriendCardUI cardPrefab;
    public Button closeButton;

    private List<FriendCardUI> pool = new List<FriendCardUI>();
    private float refreshTimer;
    private const float REFRESH_INTERVAL = 1f;

    void OnEnable()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        Refresh();
    }

    void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClose);
    }

    void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= REFRESH_INTERVAL)
        {
            refreshTimer = 0;
            Refresh();
        }
    }

    void Refresh()
    {
        var entries = new List<FriendEntry>();

        // 1) 본인
        var gm = GameManager.Instance;
        var cm = CatManager.Instance;
        if (gm != null && cm != null)
        {
            entries.Add(new FriendEntry
            {
                isMe = true,
                displayName = GetMyName(),
                catId = cm.currentCatId,
                currentStarIndex = gm.currentStarIndex,
                distanceKM = gm.distance,
                isDocked = gm.isDocked,
                status = FriendStatus.Sailing,
            });
        }

        // 2) 캐시된 모든 친구를 베이스로
        foreach (var c in FriendCache.GetAll())
        {
            var entry = new FriendEntry
            {
                steamId = c.steamId,
                displayName = GetFriendName(c.steamId),
                catId = c.currentCatId,
                currentStarIndex = c.lastStarIndex,
                distanceKM = c.lastDistanceKM,
                isDocked = c.wasDocked,
            };

            // 3) 실시간 packet 있으면 덮어쓰기 (= Sailing)
            FriendData live = (FriendSyncManager.Instance != null)
                ? FriendSyncManager.Instance.GetLiveFriend(c.steamId)
                : null;

            if (live != null)
            {
                entry.catId = live.catType;
                entry.currentStarIndex = live.currentStarIndex;
                entry.distanceKM = live.distanceKM;
                entry.isDocked = live.isDocked;
                entry.status = FriendStatus.Sailing;
            }
            else
            {
                entry.status = GetSteamStatus(c.steamId);
            }

            entries.Add(entry);
        }

        // 4) 누적거리 + 거리순 정렬 (먼 사람 위)
        foreach (var e in entries)
            e.cumulativeKM = StarDatabase.GetCumulativeDistance(e.currentStarIndex, e.distanceKM);
        entries = entries.OrderByDescending(e => e.cumulativeKM).ToList();

        // 5) 카드 풀 갱신
        while (pool.Count < entries.Count)
            pool.Add(Instantiate(cardPrefab, cardContainer));

        for (int i = 0; i < pool.Count; i++)
        {
            if (i < entries.Count)
            {
                pool[i].gameObject.SetActive(true);
                pool[i].Bind(entries[i]);
            }
            else pool[i].gameObject.SetActive(false);
        }
    }

    string GetMyName()
    {
#if !DISABLESTEAMWORKS
        if (SteamManager.Initialized)
            return SteamFriends.GetPersonaName();
#endif
        return "You";
    }

    string GetFriendName(ulong steamId)
    {
#if !DISABLESTEAMWORKS
        if (SteamManager.Initialized)
            return SteamFriends.GetFriendPersonaName(new CSteamID(steamId));
#endif
        return $"Friend {steamId}";
    }

    FriendStatus GetSteamStatus(ulong steamId)
    {
#if !DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            var state = SteamFriends.GetFriendPersonaState(new CSteamID(steamId));
            return (state == EPersonaState.k_EPersonaStateOffline)
                ? FriendStatus.Offline : FriendStatus.Online;
        }
#endif
        return FriendStatus.Offline;
    }

    void OnClose()
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.CollapseFromCollection();
        gameObject.SetActive(false);
    }
}
