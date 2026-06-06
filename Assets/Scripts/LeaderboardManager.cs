using UnityEngine;
using Steamworks;
using System;
using System.Collections.Generic;

/// <summary>
/// Steam Leaderboards 래퍼.
/// 
/// 작동:
/// - 정박 시점에 totalDistance를 Steam에 업로드
/// - LeaderboardUI에서 조회해서 표시
/// 
/// 주의:
/// - Steam Leaderboard 점수는 int32 (최대 약 21억)
/// - 우리 거리는 km 단위로 24,700조까지 가니까 오버플로
/// - "백만 km 단위"로 압축해서 저장 (24,700조 → 24.7억, int32 가능)
/// - 24,700조 / 1,000,000 = 24,700,000,000 → 32비트로는 21억까지만 → 여전히 오버
/// - 더 큰 단위로 압축: "10억 km(=Gm) 단위" → 24,700,000 (안전)
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LEADERBOARD_NAME = "td_diag1"; // API 생성 리더보드 (기존 막힌 total_distance 대신)

    /// <summary>
    /// 거리를 int32 score로 압축할 때 나누는 값.
    /// 1점 = 100만 km. 달(38만 km) 직후부터 점수 잡힘.
    /// 최대 거리(2.1조 km까지) — 데모/정식판 데네브(1.6 × 10^16) 표현 불가.
    /// 1점 = 1억 km로 안전하지만 초반 게임이 의미 없음.
    /// 
    /// 정식판에 데네브 너머 가는 컨텐츠 추가 시 SCORE_DIVISOR 조정 필요.
    /// 현재는 데모 중심으로 화성~데네브 잘 표현되는 1억 km 단위 유지.
    /// </summary>
    public const double SCORE_DIVISOR = 100_000_000.0; // 1억 km 단위

    private SteamLeaderboard_t leaderboard;
    private bool leaderboardReady;
    private float uploadTimer;
    private double lastUploadedDistance;
    private int lastUploadedScore = -1;           // 마지막으로 업로드된 압축 점수
    private float lastUploadRealtime = -9999f;     // 마지막 업로드 시각 (realtime)
    private const float MIN_UPLOAD_INTERVAL = 65f; // 최소 업로드 간격(초). Steam 제한(10분/10회) 회피

    // 콜백 결과
    private CallResult<LeaderboardFindResult_t> findResult;
    private CallResult<LeaderboardScoreUploaded_t> uploadResult;
    // Global / MyRank 다운로드는 각자 CallResult를 써야 함 (하나로 공유하면 서로 덮어써서 한쪽 콜백이 안 옴)
    private CallResult<LeaderboardScoresDownloaded_t> globalDownloadResult;
    private CallResult<LeaderboardScoresDownloaded_t> myRankDownloadResult;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[Leaderboard] Steam not initialized");
            return;
        }

        findResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
        uploadResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
        globalDownloadResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnGlobalDownloaded);
        myRankDownloadResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnMyRankDownloaded);

        // 없으면 API가 자동 생성 (Descending/Numeric). 웹에서 막힌 리더보드를 피하기 위해 FindOrCreate 사용.
        var handle = SteamUserStats.FindOrCreateLeaderboard(
            LEADERBOARD_NAME,
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
        findResult.Set(handle);
    }

    void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
    {
        if (ioFailure || result.m_bLeaderboardFound == 0)
        {
            Debug.LogWarning($"[Leaderboard] Find failed. ioFailure={ioFailure}");
            return;
        }
        leaderboard = result.m_hSteamLeaderboard;
        leaderboardReady = true;
        Debug.Log($"[Leaderboard] Ready: {LEADERBOARD_NAME}");

        // 처음 발견 시 한 번 업로드 (현재 거리)
        UploadCurrentScore();
    }

    void Update()
    {
        if (!leaderboardReady || GameManager.Instance == null) return;

        // 1분마다 자동 업로드 시도 (점수 변화/간격 체크는 UploadCurrentScore 내부에서 처리)
        uploadTimer += Time.unscaledDeltaTime;
        if (uploadTimer >= 60f)
        {
            uploadTimer = 0f;
            UploadCurrentScore();
        }
    }

    /// <summary>
    /// 현재 totalDistance를 Steam에 업로드. 더 큰 값일 때만 갱신됨 (Steam이 처리).
    /// </summary>
    public void UploadCurrentScore()
    {
        if (!leaderboardReady || GameManager.Instance == null) return;

        double km = GameManager.Instance.totalDistance;
        if (km < 0) km = 0;

        // 압축: km / divisor = int32 안전
        long compressed = (long)(km / SCORE_DIVISOR);
        if (compressed > int.MaxValue) compressed = int.MaxValue;
        int score = (int)compressed;

        // 0점은 업로드 의미 없음 (Steam이 정렬 못하고 본인 순위도 못 잡음)
        if (score <= 0)
        {
            Debug.Log($"[Leaderboard] Skip upload (score=0, distance={km:N0} km)");
            return;
        }

        // 점수가 이전보다 크지 않으면 스킵 (같은 값 재업로드 = API 낭비)
        if (score <= lastUploadedScore) return;

        // Steam 제한: 10분에 10회 + 동시 호출 1개. 최소 간격 강제로 레이트리밋 방지
        if (Time.realtimeSinceStartup - lastUploadRealtime < MIN_UPLOAD_INTERVAL) return;

        lastUploadedDistance = km;
        lastUploadedScore = score;
        lastUploadRealtime = Time.realtimeSinceStartup;

        var handle = SteamUserStats.UploadLeaderboardScore(
            leaderboard,
            ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate,
            score,
            null, 0);
        uploadResult.Set(handle);

        Debug.Log($"[Leaderboard] Uploading: {km:N0} km → score {score:N0}");
    }

    void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
    {
        if (ioFailure || result.m_bSuccess == 0)
        {
            Debug.LogWarning($"[Leaderboard] Upload failed. " +
                $"ioFailure={ioFailure} success={result.m_bSuccess} " +
                $"score={result.m_nScore} changed={result.m_bScoreChanged} " +
                $"newRank={result.m_nGlobalRankNew} prevRank={result.m_nGlobalRankPrevious}");
            // 실패 시 재시도 허용 (재시도 빈도는 MIN_UPLOAD_INTERVAL이 제한)
            lastUploadedScore = -1;
            return;
        }
        Debug.Log($"[Leaderboard] Uploaded. Global rank: {result.m_nGlobalRankNew}");
    }

    // ============ 조회 ============

    public event Action<List<LeaderboardEntry>> OnEntriesFetched;
    public event Action<LeaderboardEntry> OnMyRankFetched;

    /// <summary>
    /// 전 세계 상위 N명 조회. (본인이 상위권이면 이 목록에 포함됨)
    /// </summary>
    public void FetchGlobalTop(int count = 50)
    {
        if (!leaderboardReady) return;
        var handle = SteamUserStats.DownloadLeaderboardEntries(
            leaderboard,
            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
            1, count);
        globalDownloadResult.Set(handle);
    }

    /// <summary>
    /// 본인 한 명만 조회 (전 세계 순위 안에서 본인 위치).
    /// </summary>
    public void FetchMyRank()
    {
        if (!leaderboardReady) return;
        var handle = SteamUserStats.DownloadLeaderboardEntries(
            leaderboard,
            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
            0, 0);
        myRankDownloadResult.Set(handle);
    }

    // 다운로드 결과 → LeaderboardEntry 리스트로 변환 (공용)
    private List<LeaderboardEntry> ParseEntries(LeaderboardScoresDownloaded_t result)
    {
        var entries = new List<LeaderboardEntry>();
        for (int i = 0; i < result.m_cEntryCount; i++)
        {
            LeaderboardEntry_t e;
            SteamUserStats.GetDownloadedLeaderboardEntry(
                result.m_hSteamLeaderboardEntries, i, out e, null, 0);

            entries.Add(new LeaderboardEntry
            {
                rank = e.m_nGlobalRank,
                steamId = e.m_steamIDUser.m_SteamID,
                steamName = SteamFriends.GetFriendPersonaName(e.m_steamIDUser),
                score = e.m_nScore,
                distanceKM = (double)e.m_nScore * SCORE_DIVISOR
            });
        }
        return entries;
    }

    void OnGlobalDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
    {
        if (ioFailure)
        {
            Debug.LogWarning("[Leaderboard] Global download failed.");
            OnEntriesFetched?.Invoke(new List<LeaderboardEntry>());
            return;
        }
        var entries = ParseEntries(result);
        Debug.Log($"[Leaderboard] Downloaded {entries.Count} entries (Global)");
        OnEntriesFetched?.Invoke(entries);
    }

    void OnMyRankDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
    {
        if (ioFailure)
        {
            Debug.LogWarning("[Leaderboard] MyRank download failed.");
            OnMyRankFetched?.Invoke(null);
            return;
        }
        var entries = ParseEntries(result);
        Debug.Log($"[Leaderboard] Downloaded {entries.Count} entries (MyRank)");
        OnMyRankFetched?.Invoke(entries.Count > 0 ? entries[0] : null);
    }
}

/// <summary>
/// Leaderboard 한 줄. UI에서 표시용.
/// </summary>
public class LeaderboardEntry
{
    public int rank;
    public ulong steamId;
    public string steamName;
    public int score;       // 압축된 점수
    public double distanceKM; // 실제 km
}
