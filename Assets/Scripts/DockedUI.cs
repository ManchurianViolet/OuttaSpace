using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DockedUI : MonoBehaviour
{
    [Header("Star Info")]
    public TextMeshProUGUI starNameText;
    public TextMeshProUGUI creditsText;

    [Header("Next Destination")]
    public TextMeshProUGUI nextDestText;
    public TextMeshProUGUI etaText;

    [Header("Depart Button")]
    public Button departButton;
    public TextMeshProUGUI departButtonText;

    [Header("Game Clear (last star arrival)")]
    [Tooltip("마지막 별 도착 시 표시할 '다시하기' 버튼.")]
    public Button restartButton;
    [Tooltip("다시하기 버튼 안의 TMP 라벨.")]
    public TextMeshProUGUI restartButtonLabel;
    [Tooltip("마지막 별 도착 시 표시할 '다시하기 + 모든 고양이 잠금해제' 버튼.")]
    public Button restartUnlockButton;
    [Tooltip("잠금해제 버튼 안의 TMP 라벨.")]
    public TextMeshProUGUI restartUnlockButtonLabel;
    [Tooltip("마지막 별 도착 시 숨길 업그레이드/가챠 등 버튼들.")]
    public GameObject[] hideOnClear;

    // ETA 깜빡 경고용 (12시간 초과 시)
    private bool etaBlinking;
    private Color etaOriginalColor;
    private bool etaColorCached;

    // 평소 정박 화면의 nextDestText/etaText 원래 위치 캐시
    // 데모 종료 시 화면 중앙으로 이동했다가 일반 정박 복귀 시 원위치
    private Vector2 nextDestOriginalPos;
    private Vector2 etaOriginalPos;
    private bool textPositionsCached;

    void OnEnable()
    {
        starNameText.text = Loc.Get("loading_location");

        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.OnStateChanged += OnWindowStateChanged;
        if (departButton != null)
            departButton.onClick.AddListener(OnDepartClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (restartUnlockButton != null)
            restartUnlockButton.onClick.AddListener(OnRestartUnlockClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.OnStateChanged -= OnWindowStateChanged;
        if (departButton != null)
            departButton.onClick.RemoveListener(OnDepartClicked);
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
        if (restartUnlockButton != null)
            restartUnlockButton.onClick.RemoveListener(OnRestartUnlockClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStatsChanged -= Refresh;
    }

    void OnWindowStateChanged(WindowStateManager.WindowState state)
    {
        if (state == WindowStateManager.WindowState.Docked)
        {
            Refresh();
        }
    }

    void Update()
    {
        if (creditsText != null && GameManager.Instance != null)
            creditsText.text = GameManager.FormatNumber(GameManager.Instance.credits) + " CR";

        // ETA 24시간 초과 시 빨강 깜빡 + 크기 살짝 펌프
        if (etaBlinking && etaText != null)
        {
            // 원래 색 캐시 (한 번만)
            if (!etaColorCached)
            {
                etaOriginalColor = etaText.color;
                etaColorCached = true;
            }

            // 1초 주기로 빨강 ↔ 원래색
            float t = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            etaText.color = Color.Lerp(etaOriginalColor, new Color(1f, 0.3f, 0.3f), t);

            // 크기 1.0 ~ 1.15 사이 펌핑
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.075f;
            etaText.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        StarData star = GetDockedStar(gm);

        if (star != null && !string.IsNullOrEmpty(star.nameKey))
        {
            if (starNameText != null)
                starNameText.text = Loc.Get(star.nameKey);
        }

        if (creditsText != null)
            creditsText.text = GameManager.FormatNumber(gm.credits) + " CR";

        // 클리어 분기 — 마지막 별(M87) 도착 시
        bool isCleared = gm.currentStarIndex >= StarDatabase.Stars.Length - 1;

        // 텍스트 원래 위치 한 번만 캐시
        if (!textPositionsCached)
        {
            if (nextDestText != null)
                nextDestOriginalPos = nextDestText.rectTransform.anchoredPosition;
            if (etaText != null)
                etaOriginalPos = etaText.rectTransform.anchoredPosition;
            textPositionsCached = true;
        }

        int nextIdx = gm.currentStarIndex + 1;
        bool hasNext = nextIdx < StarDatabase.Stars.Length;

        if (isCleared)
        {
            // 클리어 — 마지막 별 도착 화면
            if (nextDestText != null)
            {
                nextDestText.text = Loc.Get("clear_title");
                nextDestText.rectTransform.anchoredPosition = new Vector2(0, 125);
                nextDestText.alignment = TextAlignmentOptions.Center;
            }
            if (etaText != null)
            {
                etaText.text = "";
                etaText.rectTransform.anchoredPosition = new Vector2(0, 50);
                etaText.alignment = TextAlignmentOptions.Center;
            }
            if (departButton != null)
                departButton.gameObject.SetActive(false);  // 출발 버튼 숨김
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
            if (restartButtonLabel != null)
                restartButtonLabel.text = Loc.Get("clear_restart");
            if (restartUnlockButton != null)
                restartUnlockButton.gameObject.SetActive(true);
            if (restartUnlockButtonLabel != null)
                restartUnlockButtonLabel.text = Loc.Get("clear_restart_unlock");

            // 업그레이드/가챠 등 보조 UI 숨김
            if (hideOnClear != null)
            {
                foreach (var obj in hideOnClear)
                    if (obj != null) obj.SetActive(false);
            }
        }
        else if (hasNext)
        {
            // 보조 UI 복원
            if (hideOnClear != null)
            {
                foreach (var obj in hideOnClear)
                    if (obj != null) obj.SetActive(true);
            }
            if (departButton != null)
                departButton.gameObject.SetActive(true);
            if (restartButton != null)
                restartButton.gameObject.SetActive(false);
            if (restartUnlockButton != null)
                restartUnlockButton.gameObject.SetActive(false);

            // 텍스트 원위치 복귀
            if (nextDestText != null)
                nextDestText.rectTransform.anchoredPosition = nextDestOriginalPos;
            if (etaText != null)
                etaText.rectTransform.anchoredPosition = etaOriginalPos;

            StarData next = StarDatabase.Stars[nextIdx];
            string nextName = Loc.Get(next.nameKey);

            // ETA 계산 → 12시간 초과면 깜빡 + 크기 변화 경고
            double avgSpeed = ComputeAverageSpeed(gm);
            double etaSeconds = avgSpeed > 0 ? next.distanceKM / avgSpeed : 0;
            etaBlinking = (etaSeconds > 43200); // 12시간

            if (!etaBlinking && etaText != null && etaColorCached)
            {
                etaText.color = etaOriginalColor;
                etaText.rectTransform.localScale = Vector3.one;
            }

            if (nextDestText != null)
                nextDestText.text = Loc.Get("dock_next", nextName, StarDatabase.FormatKM(next.distanceKM));

            if (etaText != null && avgSpeed > 0)
                etaText.text = Loc.Get("dock_eta", GameManager.FormatTime(etaSeconds));

            if (departButtonText != null)
                departButtonText.text = Loc.Get("dock_depart", nextName);

            if (departButton != null)
                departButton.interactable = true;
            if (wishlistButton != null)
                wishlistButton.gameObject.SetActive(false);
        }
        else
        {
            // 데모 아닌 정식 빌드에서 최종 도착
            if (nextDestText != null) nextDestText.text = Loc.Get("dock_last_dest");
            if (etaText != null) etaText.text = "";
            if (departButtonText != null) departButtonText.text = Loc.Get("dock_complete");
            if (departButton != null) departButton.interactable = false;
            if (wishlistButton != null) wishlistButton.gameObject.SetActive(false);
        }
    }

    double ComputeAverageSpeed(GameManager gm)
    {
        double baseSpeed = gm.GetTotalSpeed();
        if (BoosterSystem.Instance == null) return baseSpeed;

        var bs = BoosterSystem.Instance;
        float chargeTime = bs.autoFillPerSecond > 0 ? 1f / bs.autoFillPerSecond : 50f;
        float drainTime = bs.boostDrainPerSecond > 0 ? 1f / bs.boostDrainPerSecond : 5f;
        float boostMult = gm.GetBoosterSpeedMultiplier();

        double avgMult = (chargeTime + drainTime * boostMult) / (chargeTime + drainTime);
        return baseSpeed * avgMult;
    }

    StarData GetDockedStar(GameManager gm)
    {
        var wsm = WindowStateManager.Instance;
        if (wsm != null && wsm.dockedStar != null)
            return wsm.dockedStar;

        if (gm.arrivedStars.Count > 0)
        {
            int lastIdx = gm.arrivedStars[gm.arrivedStars.Count - 1];
            if (lastIdx >= 0 && lastIdx < StarDatabase.Stars.Length)
                return StarDatabase.Stars[lastIdx];
        }

        return null;
    }

    void OnDepartClicked()
    {
        if (WindowStateManager.Instance != null)
            WindowStateManager.Instance.Depart();
    }

    void OnRestartClicked()
    {
        // 전체 데이터 리셋 + 씬 재로드 (Shift+R과 동일)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void OnRestartUnlockClicked()
    {
        // 모든 고양이 ID를 임시로 저장 → 리셋 → 씬 로드 후 다시 적용
        var allCats = new System.Collections.Generic.List<int>();
        for (int i = 0; i < CatDatabase.TOTAL_COUNT; i++)
            allCats.Add(i);

        PlayerPrefs.DeleteAll();
        // 고양이 보유 정보만 미리 PlayerPrefs에 박아둠 (CatManager.LoadCats가 읽음)
        PlayerPrefs.SetString("OwnedCats", string.Join(",", allCats));
        PlayerPrefs.Save();

        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
