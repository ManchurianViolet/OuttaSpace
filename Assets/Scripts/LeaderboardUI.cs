using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Steamworks;

/// <summary>
/// 전 세계 리더보드 패널.
/// 1~10위 (이름 + Steam 아바타 + 거리) + 본인 순위/거리.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    [Header("Top 10 List")]
    [Tooltip("ScrollRect의 Content 또는 그냥 Vertical Layout Group 컨테이너")]
    public Transform entryContainer;
    [Tooltip("항목 prefab: 자식에 RawImage(아바타) + TMP 3개(rank/name/distance)")]
    public GameObject entryPrefab;

    [Header("My Row (separate, below top 10)")]
    [Tooltip("본인 순위 영역의 아바타")]
    public RawImage myAvatar;
    public TextMeshProUGUI myRankText;
    public TextMeshProUGUI myNameText;
    public TextMeshProUGUI myDistanceText;
    [Tooltip("본인 지구로부터 거리 TMP (드래그 연결)")]
    public TextMeshProUGUI myFromEarthText;

    [Header("Column Headers (CSV 키로 자동 번역)")]
    [Tooltip("순위 칸 헤더 (lb_rank)")]
    public TextMeshProUGUI rankHeaderText;
    [Tooltip("이름 칸 헤더 (lb_username)")]
    public TextMeshProUGUI nameHeaderText;
    [Tooltip("지구로부터 칸 헤더 (lb_far_from_earth)")]
    public TextMeshProUGUI fromEarthHeaderText;
    [Tooltip("점수 칸 헤더 (lb_score)")]
    public TextMeshProUGUI scoreHeaderText;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI loadingText;
    [Tooltip("X 닫기 버튼. 누르면 패널 비활성 + SettingsPanel 복귀.")]
    public Button closeButton;

    [Header("Optional")]
    [Tooltip("닫을 때 다시 보일 부모 SettingsPanel (없어도 됨)")]
    public SettingsPanelUI parentSettingsPanel;

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private ulong mySteamId;

    // 아바타 콜백 (Steam 비동기)
    protected Callback<AvatarImageLoaded_t> avatarLoadedCallback;
    private Dictionary<ulong, RawImage> pendingAvatars = new Dictionary<ulong, RawImage>();

    void Awake()
    {
        if (SteamManager.Initialized)
            avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
    }

    void OnEnable()
    {
        // 정박/위젯 모드와 무관하게 정박 크기로 확장
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.ExpandForCollection();

        if (SteamManager.Initialized)
            mySteamId = SteamUser.GetSteamID().m_SteamID;

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.OnEntriesFetched += OnEntriesFetched;
            LeaderboardManager.Instance.OnMyRankFetched += OnMyRankFetched;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (titleText != null) titleText.text = Loc.Get("lb_title");
        if (rankHeaderText != null) rankHeaderText.text = Loc.Get("lb_rank");
        if (nameHeaderText != null) nameHeaderText.text = Loc.Get("lb_username");
        if (fromEarthHeaderText != null) fromEarthHeaderText.text = Loc.Get("lb_far_from_earth");
        if (scoreHeaderText != null) scoreHeaderText.text = Loc.Get("lb_score");

        FetchAndShow();
    }

    void OnDisable()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.OnEntriesFetched -= OnEntriesFetched;
            LeaderboardManager.Instance.OnMyRankFetched -= OnMyRankFetched;
        }

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    void Close()
    {
        gameObject.SetActive(false);

        // 정박 크기에서 원래 크기로 복귀
        var wsm = WindowStateManager.Instance;
        if (wsm != null) wsm.CollapseFromCollection();

        if (parentSettingsPanel != null)
            parentSettingsPanel.Reopen();
    }

    void FetchAndShow()
    {
        ClearEntries();
        SetLoading(true);

        // 본인 영역 초기화
        if (myRankText != null) myRankText.text = "...";
        if (myDistanceText != null) myDistanceText.text = "";
        if (myFromEarthText != null) myFromEarthText.text = "";
        if (myNameText != null)
            myNameText.text = SteamManager.Initialized ? SteamFriends.GetPersonaName() : "";
        if (myAvatar != null) LoadAvatar((CSteamID)mySteamId, myAvatar);

        var mgr = LeaderboardManager.Instance;
        if (mgr == null)
        {
            SetLoading(true, Loc.Get("lb_empty"));
            return;
        }

        // 조회만 수행. 업로드는 LeaderboardManager가 자동 처리.
        // (패널 열 때마다 업로드하면 Steam 레이트리밋 10분/10회에 걸림)
        mgr.FetchGlobalTop(10);
        mgr.FetchMyRank();
    }

    void OnEntriesFetched(List<LeaderboardEntry> entries)
    {
        ClearEntries();
        SetLoading(false);

        if (entries.Count == 0)
        {
            SetLoading(true, Loc.Get("lb_empty"));
            return;
        }

        foreach (var entry in entries)
        {
            var go = Instantiate(entryPrefab, entryContainer);
            spawnedEntries.Add(go);

            // 프리팹에 LeaderboardEntryRow가 있으면 드래그 연결된 TMP 사용 (권장)
            var row = go.GetComponent<LeaderboardEntryRow>();
            if (row != null)
            {
                if (row.rankText != null) row.rankText.text = $"#{entry.rank}";
                if (row.nameText != null) row.nameText.text = entry.steamName;
                if (row.starsText != null) row.starsText.text = $"★ {entry.score}";
                if (row.fromEarthText != null)
                {
                    row.fromEarthText.richText = true; // 지수 표기(<sup>)용
                    row.fromEarthText.text = FromEarthText(entry.score);
                }
            }
            else
            {
                // 컴포넌트 없으면 기존 방식 (자식 순서: rank, name, stars)
                var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length >= 1) texts[0].text = $"#{entry.rank}";
                if (texts.Length >= 2) texts[1].text = entry.steamName;
                if (texts.Length >= 3) texts[2].text = $"★ {entry.score}";
            }

            // 아바타 (자식의 첫 번째 RawImage)
            var avatar = go.GetComponentInChildren<RawImage>(true);
            if (avatar != null)
                LoadAvatar((CSteamID)entry.steamId, avatar);
        }
    }

    /// <summary>
    /// 점수(지나간 항성 수) → 지구로부터 추정 거리 (거리만, 라벨 없음).
    /// 점수 N = N번째 별 도착 후 다음 별로 이동 중 → 누적거리 + 다음 구간의 절반으로 추정.
    /// 표기는 FormatKM 공통 사용 (조/T 초과 시 지수 표기).
    /// </summary>
    static string FromEarthText(int score)
    {
        if (score <= 0) return "";
        double est = StarDatabase.GetCumulativeDistance(score, 0)
                   + StarDatabase.GetStar(score).distanceKM * 0.5;
        return StarDatabase.FormatKM(est);
    }

    void OnMyRankFetched(LeaderboardEntry me)
    {
        if (me == null)
        {
            if (myRankText != null) myRankText.text = "-";
            if (myDistanceText != null) myDistanceText.text = "★ 0";
            if (myFromEarthText != null) myFromEarthText.text = "";
            return;
        }

        if (myRankText != null) myRankText.text = $"#{me.rank}";
        if (myNameText != null) myNameText.text = me.steamName;
        if (myDistanceText != null) myDistanceText.text = $"★ {me.score}";
        if (myFromEarthText != null)
        {
            myFromEarthText.richText = true; // 지수 표기(<sup>)용
            myFromEarthText.text = FromEarthText(me.score);
        }
    }

    // ============ Steam Avatar ============

    /// <summary>
    /// Steam 사용자 아바타 로드. 비동기. AvatarImageLoaded_t 콜백 통해 적용됨.
    /// </summary>
    void LoadAvatar(CSteamID steamId, RawImage targetImage)
    {
        if (!SteamManager.Initialized) return;

        int imageHandle = SteamFriends.GetMediumFriendAvatar(steamId);
        if (imageHandle == -1)
        {
            // 아직 로드 안 됨 — Steam이 백그라운드에서 다운로드 후 콜백 발행
            pendingAvatars[steamId.m_SteamID] = targetImage;
            return;
        }
        if (imageHandle == 0) return; // 아바타 없음

        ApplyAvatarImage(imageHandle, targetImage);
    }

    void OnAvatarLoaded(AvatarImageLoaded_t param)
    {
        ulong sid = param.m_steamID.m_SteamID;
        if (pendingAvatars.TryGetValue(sid, out var img) && img != null)
        {
            ApplyAvatarImage(param.m_iImage, img);
            pendingAvatars.Remove(sid);
        }
    }

    void ApplyAvatarImage(int imageHandle, RawImage target)
    {
        if (target == null) return;

        uint w, h;
        if (!SteamUtils.GetImageSize(imageHandle, out w, out h)) return;
        if (w == 0 || h == 0) return;

        int byteCount = (int)(w * h * 4);
        byte[] buffer = new byte[byteCount];
        if (!SteamUtils.GetImageRGBA(imageHandle, buffer, byteCount)) return;

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        // Steam은 위에서 아래로 정렬 → Unity는 아래에서 위로 → flip
        var flipped = new byte[byteCount];
        int rowSize = (int)w * 4;
        for (int y = 0; y < h; y++)
        {
            System.Array.Copy(buffer, y * rowSize,
                flipped, (h - 1 - y) * rowSize, rowSize);
        }
        tex.LoadRawTextureData(flipped);
        tex.Apply();
        target.texture = tex;
    }

    void ClearEntries()
    {
        foreach (var go in spawnedEntries) if (go != null) Destroy(go);
        spawnedEntries.Clear();
        pendingAvatars.Clear();
    }

    void SetLoading(bool show, string text = null)
    {
        if (loadingText == null) return;
        loadingText.gameObject.SetActive(show);
        if (show) loadingText.text = text ?? Loc.Get("loading");
    }
}
