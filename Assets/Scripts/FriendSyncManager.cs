using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Steam 친구와 위치 데이터를 P2P로 교환.
/// 2초마다 자기 상태를 친구들에게 전송, 친구 데이터 수신.
/// Rich Presence로 친구 목록에 현재 상태 표시.
/// 받은 packet은 FriendCache에도 영구 저장됨.
/// </summary>
public class FriendSyncManager : MonoBehaviour
{
    public static FriendSyncManager Instance { get; private set; }

    [Header("Settings")]
    public float syncInterval = 2f;
    public float dataExpireTime = 10f;

    [Header("Visibility Rule")]
    [Tooltip("같은 구간일 때, 현재 별 거리의 이 비율(0~1) 이내면 화면에 표시. 기본 0.5 = 절반")]
    public double nearbyDistanceRatio = 0.5;

    public Dictionary<ulong, FriendData> friendDataMap = new Dictionary<ulong, FriendData>();

    private float syncTimer;

#if !DISABLESTEAMWORKS
    private Callback<P2PSessionRequest_t> p2pSessionRequestCallback;
    const int CHANNEL_SYNC = 0;
    const uint PACKET_MAGIC = 0x4F555454;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!SteamManager.Initialized) return;
        p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        UpdateRichPresence();
    }

    void Update()
    {
        if (!SteamManager.Initialized) return;
        ReceivePackets();

        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            syncTimer = 0f;
            SendMyData();
            UpdateRichPresence();
            CleanExpiredData();
        }
    }

    // ============ 전송 ============

    void SendMyData()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        byte[] data = SerializeMyData(gm);

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            EPersonaState state = SteamFriends.GetFriendPersonaState(friendId);
            if (state == EPersonaState.k_EPersonaStateOffline) continue;

            FriendGameInfo_t gameInfo;
            if (!SteamFriends.GetFriendGamePlayed(friendId, out gameInfo)) continue;
            if (gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) continue;

            SteamNetworking.SendP2PPacket(
                friendId,
                data,
                (uint)data.Length,
                EP2PSend.k_EP2PSendUnreliable,
                CHANNEL_SYNC
            );
        }
    }

    byte[] SerializeMyData(GameManager gm)
    {
        int myCatId = (CatManager.Instance != null) ? CatManager.Instance.currentCatId : 0;

        using (MemoryStream ms = new MemoryStream(40))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(PACKET_MAGIC);
            bw.Write(SteamUser.GetSteamID().m_SteamID);
            bw.Write(gm.currentStarIndex);
            bw.Write(gm.distance);
            bw.Write(gm.GetEffectiveSpeed());
            bw.Write(myCatId);
            bw.Write(gm.isDocked ? (byte)1 : (byte)0);
            return ms.ToArray();
        }
    }

    // ============ 수신 ============

    void ReceivePackets()
    {
        uint packetSize;
        while (SteamNetworking.IsP2PPacketAvailable(out packetSize, CHANNEL_SYNC))
        {
            byte[] buffer = new byte[packetSize];
            uint bytesRead;
            CSteamID senderId;

            if (SteamNetworking.ReadP2PPacket(buffer, packetSize, out bytesRead, out senderId, CHANNEL_SYNC))
            {
                DeserializeAndStore(buffer, senderId);
            }
        }
    }

    void DeserializeAndStore(byte[] data, CSteamID senderId)
    {
        try
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                uint magic = br.ReadUInt32();
                if (magic != PACKET_MAGIC) return;

                ulong steamId = br.ReadUInt64();
                int starIndex = br.ReadInt32();
                double distanceKM = br.ReadDouble();
                double speedKMS = br.ReadDouble();
                int catType = br.ReadInt32();
                bool isDocked = br.ReadByte() == 1;

                string friendName = SteamFriends.GetFriendPersonaName(senderId);

                FriendData fd = new FriendData
                {
                    steamId = steamId,
                    friendName = friendName,
                    currentStarIndex = starIndex,
                    distanceKM = distanceKM,
                    speedKMS = speedKMS,
                    catType = catType,
                    isDocked = isDocked,
                    lastUpdateTime = Time.time
                };

                friendDataMap[steamId] = fd;
                FriendCache.Update(steamId, catType, starIndex, distanceKM, isDocked);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FriendSync] 패킷 파싱 실패: {e.Message}");
        }
    }

    // ============ P2P 세션 ============

    void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        EFriendRelationship rel = SteamFriends.GetFriendRelationship(request.m_steamIDRemote);
        if (rel == EFriendRelationship.k_EFriendRelationshipFriend)
        {
            SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
        }
    }

    // ============ Rich Presence ============

    void UpdateRichPresence()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        StarData star = StarDatabase.Stars[gm.currentStarIndex];
        string starName = Loc.Get(star.nameKey);

        if (gm.isDocked)
        {
            SteamFriends.SetRichPresence("status", Loc.Get("hud_arrived_at", starName));
        }
        else
        {
            float pct = (float)(gm.distance / star.distanceKM) * 100f;
            SteamFriends.SetRichPresence("status",
                $"{Loc.Get("hud_heading_to", starName)} ({pct:F0}%)");
        }

        SteamFriends.SetRichPresence("steam_display", "#Status");
    }

    // ============ 유틸 ============

    void CleanExpiredData()
    {
        List<ulong> toRemove = new List<ulong>();
        foreach (var kvp in friendDataMap)
        {
            if (Time.time - kvp.Value.lastUpdateTime > dataExpireTime)
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
            friendDataMap.Remove(id);
    }

    /// <summary>
    /// 화면에 표시할 만큼 가까운 친구 목록.
    /// 룰: 같은 구간 + 거리차가 (현재 구간 거리 × nearbyDistanceRatio) 이내 + 항해 중(정박 X).
    /// 정박한 친구는 행성에 머물러 있으므로 본인 옆에서 함께 나는 모습으로 표시하지 않음.
    /// (친구창에는 GetLiveFriend로 별도 표시됨)
    /// </summary>
    public List<FriendData> GetNearbyFriends()
    {
        List<FriendData> nearby = new List<FriendData>();
        var gm = GameManager.Instance;
        if (gm == null) return nearby;

        double thresholdKM = GetNearbyThresholdKM();

        foreach (var kvp in friendDataMap)
        {
            FriendData fd = kvp.Value;
            if (fd.currentStarIndex != gm.currentStarIndex) continue;
            if (fd.isDocked) continue; // 정박한 친구는 화면 표시 제외

            double diff = Math.Abs(fd.distanceKM - gm.distance);
            if (diff <= thresholdKM) nearby.Add(fd);
        }

        return nearby;
    }

    /// <summary>
    /// 현재 화면 표시 임계값(km). FriendCatDisplay가 화면 매핑에 사용.
    /// </summary>
    public double GetNearbyThresholdKM()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 1.0;
        int starIdx = Mathf.Clamp(gm.currentStarIndex, 0, StarDatabase.Stars.Length - 1);
        return StarDatabase.Stars[starIdx].distanceKM * nearbyDistanceRatio;
    }

    public List<FriendData> GetAllLiveFriends()
    {
        return new List<FriendData>(friendDataMap.Values);
    }

    public FriendData GetLiveFriend(ulong steamId)
    {
        return friendDataMap.TryGetValue(steamId, out var fd) ? fd : null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#else
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<FriendData> GetNearbyFriends() => new List<FriendData>();
    public double GetNearbyThresholdKM() => 1.0;
    public List<FriendData> GetAllLiveFriends() => new List<FriendData>();
    public FriendData GetLiveFriend(ulong steamId) => null;
#endif
}

/// <summary>
/// 친구 동기화 데이터 구조.
/// </summary>
[System.Serializable]
public class FriendData
{
    public ulong steamId;
    public string friendName;
    public int currentStarIndex;
    public double distanceKM;
    public double speedKMS;
    public int catType;
    public bool isDocked;
    public float lastUpdateTime;
}