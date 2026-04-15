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
/// </summary>
public class FriendSyncManager : MonoBehaviour
{
    public static FriendSyncManager Instance { get; private set; }

    [Header("Settings")]
    public float syncInterval = 2f;          // 전송 주기 (초)
    public float dataExpireTime = 10f;       // 이 시간 동안 수신 없으면 제거
    public double nearbyRangeKM = 50000;     // 5만km 이내면 화면에 표시

    /// <summary>
    /// 수신된 친구 데이터. FriendCatDisplay에서 읽어감.
    /// Key = Steam ID (ulong)
    /// </summary>
    public Dictionary<ulong, FriendData> friendDataMap = new Dictionary<ulong, FriendData>();

    private float syncTimer;

#if !DISABLESTEAMWORKS
    // P2P 세션 요청 콜백
    private Callback<P2PSessionRequest_t> p2pSessionRequestCallback;

    // 패킷 채널
    const int CHANNEL_SYNC = 0;

    // 패킷 헤더 (다른 게임과 구분)
    const uint PACKET_MAGIC = 0x4F555454; // "OUTT"

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!SteamManager.Initialized) return;

        // P2P 연결 요청 자동 수락
        p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

        // 초기 Rich Presence 설정
        UpdateRichPresence();
    }

    void Update()
    {
        if (!SteamManager.Initialized) return;

        // 수신 처리
        ReceivePackets();

        // 주기적 전송
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

        // 온라인 친구들에게 전송
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            // 온라인 상태인 친구에게만 전송
            EPersonaState state = SteamFriends.GetFriendPersonaState(friendId);
            if (state == EPersonaState.k_EPersonaStateOffline) continue;

            // 같은 게임을 하고 있는 친구에게만 전송
            FriendGameInfo_t gameInfo;
            if (!SteamFriends.GetFriendGamePlayed(friendId, out gameInfo)) continue;
            if (gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) continue;

            SteamNetworking.SendP2PPacket(
                friendId,
                data,
                (uint)data.Length,
                EP2PSend.k_EP2PSendUnreliable, // 위치 데이터는 unreliable이 적합 (빠르고 가벼움)
                CHANNEL_SYNC
            );
        }
    }

    byte[] SerializeMyData(GameManager gm)
    {
        using (MemoryStream ms = new MemoryStream(40))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(PACKET_MAGIC);                          // 4 bytes: 매직넘버
            bw.Write(SteamUser.GetSteamID().m_SteamID);      // 8 bytes: 내 Steam ID
            bw.Write(gm.currentStarIndex);                   // 4 bytes: 현재 구간
            bw.Write(gm.distance);                           // 8 bytes: 현재 거리 (km)
            bw.Write(gm.GetEffectiveSpeed());                // 8 bytes: 현재 속도 (km/s)
            bw.Write(0);                                     // 4 bytes: 고양이 타입 (나중에 구현)
            bw.Write(gm.isDocked ? (byte)1 : (byte)0);      // 1 byte: 정박 여부
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
                if (magic != PACKET_MAGIC) return; // 다른 게임 패킷 무시

                ulong steamId = br.ReadUInt64();
                int starIndex = br.ReadInt32();
                double distanceKM = br.ReadDouble();
                double speedKMS = br.ReadDouble();
                int catType = br.ReadInt32();
                bool isDocked = br.ReadByte() == 1;

                // 친구 이름 가져오기
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
        // 친구인지 확인 후 수락
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
            SteamFriends.SetRichPresence("status",
                Loc.Get("hud_arrived_at", starName));
        }
        else
        {
            string dist = StarDatabase.FormatKM(gm.distance);
            string total = StarDatabase.FormatKM(star.distanceKM);
            float pct = (float)(gm.distance / star.distanceKM) * 100f;

            SteamFriends.SetRichPresence("status",
                $"{Loc.Get("hud_heading_to", starName)} ({pct:F0}%)");
        }

        SteamFriends.SetRichPresence("steam_display", "#Status");
    }

    // ============ 유틸 ============

    /// <summary>
    /// 오래된 친구 데이터 제거 (연결 끊긴 친구)
    /// </summary>
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
    /// 현재 근처에 있는 친구 목록 반환.
    /// FriendCatDisplay에서 호출.
    /// </summary>
    public List<FriendData> GetNearbyFriends()
    {
        List<FriendData> nearby = new List<FriendData>();
        var gm = GameManager.Instance;
        if (gm == null) return nearby;

        foreach (var kvp in friendDataMap)
        {
            FriendData fd = kvp.Value;

            // 같은 구간에 있는지
            if (fd.currentStarIndex != gm.currentStarIndex) continue;

            // 거리 차이 계산
            double diff = Math.Abs(fd.distanceKM - gm.distance);
            if (diff <= nearbyRangeKM)
            {
                nearby.Add(fd);
            }
        }

        return nearby;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#else
    // Steamworks 없을 때 (에디터 테스트용)
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Dictionary<ulong, FriendData> friendDataMap = new Dictionary<ulong, FriendData>();

    public List<FriendData> GetNearbyFriends()
    {
        return new List<FriendData>();
    }
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
    public float lastUpdateTime;    // Time.time 기준
}
