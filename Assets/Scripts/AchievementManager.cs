using UnityEngine;
using Steamworks;
using System.Collections.Generic;

/// <summary>
/// Steam 도전과제 관리.
///
/// 사용:
/// - AchievementManager.Unlock("ACH_REACH_MOON") 한 줄로 해제
/// - 이미 해제된 건 중복 호출해도 Steam이 무시 (그래도 로컬 캐시로 API 낭비 방지)
/// - 별 도착/가챠/업그레이드/크레딧 등 이벤트 시점에 CheckAll() 또는 개별 Unlock 호출
///
/// 주의:
/// - Steamworks 포털에 동일한 API ID로 도전과제를 먼저 등록해야 실제로 뜸
/// - 에디터에선 Steam 연동 제한적 — 빌드에서 확인
/// - 진행형(누적 크레딧/만렙)은 매 프레임이 아니라 이벤트 시점에만 검사
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // ===== 도전과제 API ID (Steamworks 포털 등록값과 일치해야 함) =====
    public const string REACH_MOON       = "ACH_REACH_MOON";       // 1 푸른 점을 떠나
    public const string REACH_PLUTO      = "ACH_REACH_PLUTO";      // 2 태양계를 넘어
    public const string REACH_ANDROMEDA  = "ACH_REACH_ANDROMEDA";  // 3 두 은하가 만나는 곳
    public const string REACH_M87        = "ACH_REACH_M87";        // 4 여정의 끝
    public const string REACH_ABYSS      = "ACH_REACH_ABYSS";      // 5 심연이 손짓하는
    public const string ABYSS_50         = "ACH_ABYSS_50";         // 6 돌아올 수 없는 곳에서
    public const string FIRST_GACHA      = "ACH_FIRST_GACHA";      // 7 운명적인 첫 만남
    public const string COLLECT_COMMON   = "ACH_COLLECT_COMMON";   // 8 작은 별들의 친구
    public const string COLLECT_RARE     = "ACH_COLLECT_RARE";     // 9 빛나는 인연
    public const string COLLECT_LEGENDARY= "ACH_COLLECT_LEGENDARY";// 10 전설을 품다
    public const string COLLECT_ALL      = "ACH_COLLECT_ALL";      // 11 우주 낙원
    public const string MAX_UPGRADE      = "ACH_MAX_UPGRADE";      // 12 한계를 넘어서
    public const string CREDITS_1M       = "ACH_CREDITS_1M";       // 13 별보다 많은 동전
    public const string LIGHTSPEED       = "ACH_LIGHTSPEED";       // 14 빛을 앞지르다
    public const string VIEW_CREDITS     = "ACH_VIEW_CREDITS";     // 15 우리가 만든 우주

    // 판정용 상수
    private const int ABYSS_50_INDEX = 149;        // 심연 50번째 별 = ABYSS-050 = index 149 (100 + 49)
    private const double CREDITS_GOAL = 1_000_000;  // 누적 크레딧 목표
    private const double LIGHT_SPEED_KMS = 299792.458; // 빛의 속도 km/s

    // 누적 크레딧 진행도 Stat (Steamworks 포털에 INT형 Stat으로 등록 + ACH_CREDITS_1M에 연결)
    private const string STAT_TOTAL_CREDITS = "STAT_TOTAL_CREDITS";
    private float creditStatTimer;
    private int lastPushedCreditStat = -1;

    // 이미 해제된 것 로컬 캐시 (Steam 왕복 줄이기)
    private readonly HashSet<string> unlocked = new HashSet<string>();

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
            Debug.LogWarning("[Achievement] Steam not initialized — 도전과제 비활성");
            return;
        }

        // Steam은 게임 시작 시 현재 유저 stats/achievement를 자동 캐시함.
        // 이미 해제된 것들을 로컬 캐시에 반영 (소급/중복 방지)
        foreach (var id in AllIds())
        {
            bool got;
            if (SteamUserStats.GetAchievement(id, out got) && got)
                unlocked.Add(id);
        }

        // 시작 시 현재 진행 상태 기준으로 한 번 점검 (놓친 것 보정)
        CheckAll();
    }

    void Update()
    {
        if (!SteamManager.Initialized || GameManager.Instance == null) return;

        // 누적 크레딧 진행도 Stat: 5초마다, 값이 바뀐 경우에만 업로드 (진행 막대용)
        creditStatTimer += Time.unscaledDeltaTime;
        if (creditStatTimer >= 5f)
        {
            creditStatTimer = 0f;
            // 목표 도달 후엔 더 올릴 필요 없음
            int cur = (int)System.Math.Min(GameManager.Instance.totalCredits, CREDITS_GOAL);
            if (cur != lastPushedCreditStat)
            {
                lastPushedCreditStat = cur;
                SteamUserStats.SetStat(STAT_TOTAL_CREDITS, cur);
                SteamUserStats.StoreStats();
            }
        }
    }

    private static IEnumerable<string> AllIds()
    {
        yield return REACH_MOON; yield return REACH_PLUTO; yield return REACH_ANDROMEDA;
        yield return REACH_M87; yield return REACH_ABYSS; yield return ABYSS_50;
        yield return FIRST_GACHA; yield return COLLECT_COMMON; yield return COLLECT_RARE;
        yield return COLLECT_LEGENDARY; yield return COLLECT_ALL; yield return MAX_UPGRADE;
        yield return CREDITS_1M; yield return LIGHTSPEED; yield return VIEW_CREDITS;
    }

    /// <summary>도전과제 해제 (한 줄 호출용). 이미 해제됐으면 무시.</summary>
    public void Unlock(string id)
    {
        if (!SteamManager.Initialized) return;
        if (unlocked.Contains(id)) return;

        unlocked.Add(id);
        SteamUserStats.SetAchievement(id);
        SteamUserStats.StoreStats(); // 즉시 저장 → 팝업 표시
        Debug.Log($"[Achievement] Unlocked: {id}");
    }

    // ============ 이벤트 훅 ============

    /// <summary>별 도착 시 호출 (GameManager.ArriveAtStar).</summary>
    public void OnStarArrived(int starIndex)
    {
        if (starIndex >= 0) Unlock(REACH_MOON);                  // idx 0 = 달
        if (starIndex >= 6) Unlock(REACH_PLUTO);                 // idx 6 = 명왕성
        if (starIndex >= 89) Unlock(REACH_ANDROMEDA);            // idx 89 = 안드로메다
        if (starIndex >= StarDatabase.LAST_PLANNED_INDEX) Unlock(REACH_M87); // idx 99 = M87
        if (starIndex >= StarDatabase.LAST_PLANNED_INDEX + 1) Unlock(REACH_ABYSS); // idx 100 = ABYSS-001
        if (starIndex >= ABYSS_50_INDEX) Unlock(ABYSS_50);       // idx 149 = ABYSS-050
    }

    /// <summary>가챠 뽑을 때 호출 (GachaSystem).</summary>
    public void OnGachaDrawn()
    {
        Unlock(FIRST_GACHA);
    }

    /// <summary>고양이 도감 변동 시 호출 (CatManager.AddFromGacha).</summary>
    public void CheckCollection()
    {
        var cm = CatManager.Instance;
        if (cm == null) return;

        int c = CountOwned(CatRarity.Common);
        int r = CountOwned(CatRarity.Rare);
        int l = CountOwned(CatRarity.Legendary);
        Debug.Log($"[Achievement] Collection: Common {c}/{CatDatabase.COMMON_COUNT}, Rare {r}/{CatDatabase.RARE_COUNT}, Legendary {l}/{CatDatabase.LEGENDARY_COUNT}, Total OwnedCount={cm.OwnedCount}/{CatDatabase.TOTAL_COUNT} | ids=[{string.Join(",", SortedOwned())}]");

        if (c >= CatDatabase.COMMON_COUNT) Unlock(COLLECT_COMMON);
        if (r >= CatDatabase.RARE_COUNT) Unlock(COLLECT_RARE);
        if (l >= CatDatabase.LEGENDARY_COUNT) Unlock(COLLECT_LEGENDARY);
        if (c + r + l >= CatDatabase.TOTAL_COUNT) Unlock(COLLECT_ALL);
    }

    private System.Collections.Generic.List<int> SortedOwned()
    {
        var list = new System.Collections.Generic.List<int>(CatManager.Instance.ownedCats);
        list.Sort();
        return list;
    }

    /// <summary>업그레이드 구매 시 호출 (GameManager.BuyUpgrade).</summary>
    public void CheckMaxUpgrade()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        // maxLevel(100) 도달 기준. 무한모드로 캡 해제돼도 100 이상이면 달성.
        if (gm.GetUpgradeLevel(UpgradeType.Speed) >= 100
            && gm.GetUpgradeLevel(UpgradeType.BoosterSpeed) >= 100)
            Unlock(MAX_UPGRADE);
    }

    /// <summary>누적 크레딧 변동 시 호출.</summary>
    public void CheckCredits(double totalCredits)
    {
        if (totalCredits >= CREDITS_GOAL) Unlock(CREDITS_1M);
    }

    /// <summary>현재 속도(km/s) 기준 광속 돌파 체크. ProcessTick에서 호출.</summary>
    public void CheckSpeed(double currentSpeedKmS)
    {
        if (currentSpeedKmS >= LIGHT_SPEED_KMS) Unlock(LIGHTSPEED);
    }

    /// <summary>제작진 크레딧 화면 열 때 호출 (CreditsUI).</summary>
    public void OnCreditsViewed()
    {
        Unlock(VIEW_CREDITS);
    }

    /// <summary>해당 등급 보유 수.</summary>
    private int CountOwned(CatRarity rarity)
    {
        var cm = CatManager.Instance;
        if (cm == null) return 0;
        int start = CatDatabase.GetRarityStartIndex(rarity);
        int count = CatDatabase.GetRarityCount(rarity);
        int owned = 0;
        for (int i = 0; i < count; i++)
            if (cm.IsOwned(start + i)) owned++;
        return owned;
    }

    /// <summary>전체 일괄 점검 (시작 시 / 보정용).</summary>
    public void CheckAll()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 도착 기반: arrivedStars 최대값으로 판정
        int maxArrived = -1;
        foreach (int i in gm.arrivedStars)
            if (i > maxArrived) maxArrived = i;
        if (maxArrived >= 0) OnStarArrived(maxArrived);

        CheckCollection();
        CheckMaxUpgrade();
        CheckCredits(gm.totalCredits);
    }
}
