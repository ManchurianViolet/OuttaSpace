using UnityEngine;
using System;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public double credits;
    public double totalCredits;
    public double distance;
    public double totalDistance;
    public int currentStarIndex;
    public List<int> arrivedStars = new List<int>();
    public bool isDocked;

    [Header("First Time")]
    [Tooltip("첫 실행 시 지구 출발 인트로를 봤는지. 한 번만 실행됨.")]
    public bool hasSeenIntro;

    [Header("Upgrade Levels")]
    public int speedLevel;
    public int boosterSpdLevel;

    [Header("Settings")]
    public float tickRate = 0.05f;

    public event Action<StarData> OnStarArrived;
    public event Action OnStatsChanged;
    public event Action<string> OnNotification;

    private float tickTimer;
    private float saveTimer;

    private const double BASE_SPEED_COMMON = 3000;
    private const double BASE_SPEED_RARE = 10000;
    private const double BASE_SPEED_LEGENDARY = 100000;
    private const double SPEED_NORMALIZE = 3000;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 30;
        LoadGame();
    }

    void Start()
    {
        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.fuel = PlayerPrefs.GetFloat("BoosterFuel", 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            SetLanguage("ko");
        if (Input.GetKeyDown(KeyCode.F2))
            SetLanguage("en");

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaveGame();
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftShift))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Destroy(gameObject);
            Instance = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // 디버그: Shift+U → 모든 고양이 언락
        if (Input.GetKeyDown(KeyCode.U) && Input.GetKey(KeyCode.LeftShift))
        {
            if (CatManager.Instance != null)
            {
                for (int i = 0; i < CatDatabase.TOTAL_COUNT; i++)
                    CatManager.Instance.AddFromGacha(i);
                Debug.Log($"[Debug] 모든 고양이 {CatDatabase.TOTAL_COUNT}마리 언락");
            }
        }

        // 디버그: Shift+J → 다음 행성 30만 km 전으로 점프
        if (Input.GetKeyDown(KeyCode.J) && Input.GetKey(KeyCode.LeftShift))
        {
            if (currentStarIndex < StarDatabase.Stars.Length)
            {
                double target = StarDatabase.Stars[currentStarIndex].distanceKM - 300000;
                if (target < 0) target = 0;
                distance = target;
                NotifyStatsChanged();
                Debug.Log($"[Debug] 다음 행성 30만 km 전으로 점프 (distance={distance})");
            }
        }

        // 디버그: Shift+C → 크레딧 +10000
        if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftShift))
        {
            credits += 10000;
            totalCredits += 10000;
            NotifyStatsChanged();
            Debug.Log($"[Debug] +10000 CR (현재 {credits:N0})");
        }

        // 디버그: Shift+F → 가짜 친구 3명 추가 (멀티 표시 테스트용)
        if (Input.GetKeyDown(KeyCode.F) && Input.GetKey(KeyCode.LeftShift))
        {
            if (FriendSyncManager.Instance != null)
            {
                // 거리 차이를 다르게 해서 슬롯 분산 효과 확인
                string[] names = { "Tester_A", "Tester_B", "Tester_C" };
                double[] offsets = { 1000, -3000, 8000 };
                int[] catTypes = { 0, 27, 36 }; // common, rare, legendary

                for (int i = 0; i < names.Length; i++)
                {
                    var fakeFriend = new FriendData
                    {
                        steamId = (ulong)(99001 + i),
                        friendName = names[i],
                        currentStarIndex = currentStarIndex,
                        distanceKM = distance + offsets[i],
                        speedKMS = 3000,
                        catType = catTypes[i],
                        isDocked = false,
                        lastUpdateTime = Time.time
                    };
                    FriendSyncManager.Instance.friendDataMap[fakeFriend.steamId] = fakeFriend;
                }
                Debug.Log($"[Debug] 가짜 친구 3명 추가. starIdx={currentStarIndex} myDist={distance:F0}");
            }
        }

        if (isDocked && Input.GetKeyDown(KeyCode.Space))
        {
            DepartToNextStar();
            if (WindowStateManager.Instance != null)
                WindowStateManager.Instance.Depart();
        }

        tickTimer += Time.deltaTime;
        while (tickTimer >= tickRate)
        {
            tickTimer -= tickRate;
            ProcessTick();
        }

        saveTimer += Time.deltaTime;
        if (saveTimer >= 30f)
        {
            saveTimer = 0f;
            SaveGame();
        }
    }
    void SetLanguage(string code)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == code)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"언어 변경: {code}");
                OnStatsChanged?.Invoke();
                return;
            }
        }
    }
    void OnApplicationFocus(bool hasFocus)
    {
        Application.targetFrameRate = hasFocus ? 30 : 10;
        if (!hasFocus) SaveGame();
    }

    void ProcessTick()
    {
        if (isDocked) return;

        double baseSpeed = GetTotalSpeed();
        float boostMult = 1f;
        if (BoosterSystem.Instance != null)
            boostMult = BoosterSystem.Instance.GetSpeedMultiplier();

        double effectiveSpeed = baseSpeed * boostMult;
        double distGain = effectiveSpeed * tickRate;
        double creditGain = effectiveSpeed * 0.00001 * tickRate;

        distance += distGain;
        totalDistance += distGain;

        StarData current = StarDatabase.Stars[currentStarIndex];
        if (distance >= current.distanceKM && !arrivedStars.Contains(currentStarIndex))
            ArriveAtStar(currentStarIndex);

        OnStatsChanged?.Invoke();
    }

    void ArriveAtStar(int index)
    {
        StarData star = StarDatabase.Stars[index];
        arrivedStars.Add(index);
        isDocked = true;

        OnStarArrived?.Invoke(star);
        OnNotification?.Invoke(Loc.Get("notif_arrived", Loc.Get(star.nameKey)));
        SaveGame();
    }

    public void DepartToNextStar()
    {
        if (currentStarIndex < StarDatabase.Stars.Length - 1)
            currentStarIndex++;
        distance = 0;
        isDocked = false;
        OnStatsChanged?.Invoke();
    }

    // ============ SPEED ============

    public double GetRarityBaseSpeed()
    {
        if (CatManager.Instance == null || CatDatabase.Instance == null)
            return BASE_SPEED_COMMON;

        CatData cat = CatDatabase.Instance.Get(CatManager.Instance.currentCatId);
        if (cat == null) return BASE_SPEED_COMMON;

        switch (cat.rarity)
        {
            case CatRarity.Rare: return BASE_SPEED_RARE;
            case CatRarity.Legendary: return BASE_SPEED_LEGENDARY;
            default: return BASE_SPEED_COMMON;
        }
    }

    public double GetTotalSpeed()
    {
        double baseSpeed = GetRarityBaseSpeed();
        double multiplier = 1.0 + GetUpgradeContribution(UpgradeType.Speed) / SPEED_NORMALIZE;
        return baseSpeed * multiplier;
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
    public double GetEffectiveSpeed()
    {
        double baseSpeed = GetTotalSpeed();
        float boostMult = 1f;
        if (BoosterSystem.Instance != null)
            boostMult = BoosterSystem.Instance.GetSpeedMultiplier();
        return baseSpeed * boostMult;
    }

    public float GetBoosterSpeedMultiplier()
    {
        return 3f + (float)GetUpgradeContribution(UpgradeType.BoosterSpeed);
    }

    public double GetUpgradeContribution(UpgradeType type)
    {
        UpgradeData data = UpgradeDatabase.Get(type);
        int level = GetUpgradeLevel(type);
        if (level == 0) return 0;
        return data.baseEffect * (Math.Pow(data.effectMult, level) - 1) / (data.effectMult - 1);
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Speed => speedLevel,
            UpgradeType.BoosterSpeed => boosterSpdLevel,
            _ => 0
        };
    }

    // ============ UPGRADES ============

    public double GetUpgradeCost(UpgradeType type)
    {
        UpgradeData data = UpgradeDatabase.Get(type);
        int level = GetUpgradeLevel(type);
        double cost = Math.Floor(data.baseCost * Math.Pow(data.costMult, level));
        // 상한 적용 (0이면 무제한)
        if (data.maxCost > 0 && cost > data.maxCost)
            cost = data.maxCost;
        return cost;
    }

    public bool CanAfford(UpgradeType type) => credits >= GetUpgradeCost(type);

    public bool BuyUpgrade(UpgradeType type)
    {
        double cost = GetUpgradeCost(type);
        if (credits < cost) return false;
        credits -= cost;
        switch (type)
        {
            case UpgradeType.Speed: speedLevel++; break;
            case UpgradeType.BoosterSpeed: boosterSpdLevel++; break;
        }
        OnStatsChanged?.Invoke();
        return true;
    }

    // ============ SAVE / LOAD ============

    public void SaveGame()
    {
        PlayerPrefs.SetString("Credits", credits.ToString("R"));
        PlayerPrefs.SetString("TotalCredits", totalCredits.ToString("R"));
        PlayerPrefs.SetString("Distance", distance.ToString("R"));
        PlayerPrefs.SetString("TotalDistance", totalDistance.ToString("R"));
        PlayerPrefs.SetInt("CurrentStar", currentStarIndex);
        PlayerPrefs.SetString("ArrivedStars", string.Join(",", arrivedStars));
        PlayerPrefs.SetInt("SpeedLevel", speedLevel);
        PlayerPrefs.SetInt("BoosterSpdLevel", boosterSpdLevel);
        PlayerPrefs.SetInt("IsDocked", isDocked ? 1 : 0);
        PlayerPrefs.SetInt("HasSeenIntro", hasSeenIntro ? 1 : 0);
        if (BoosterSystem.Instance != null)
            PlayerPrefs.SetFloat("BoosterFuel", BoosterSystem.Instance.fuel);
        PlayerPrefs.Save();
    }

    void LoadGame()
    {
        credits = double.Parse(PlayerPrefs.GetString("Credits", "0"));
        totalCredits = double.Parse(PlayerPrefs.GetString("TotalCredits", "0"));
        distance = double.Parse(PlayerPrefs.GetString("Distance", "0"));
        totalDistance = double.Parse(PlayerPrefs.GetString("TotalDistance", "0"));
        currentStarIndex = PlayerPrefs.GetInt("CurrentStar", 0);
        speedLevel = PlayerPrefs.GetInt("SpeedLevel", 0);
        boosterSpdLevel = PlayerPrefs.GetInt("BoosterSpdLevel", 0);
        isDocked = PlayerPrefs.GetInt("IsDocked", 0) == 1;
        hasSeenIntro = PlayerPrefs.GetInt("HasSeenIntro", 0) == 1;

        string arrivedStr = PlayerPrefs.GetString("ArrivedStars", "");
        arrivedStars.Clear();
        if (!string.IsNullOrEmpty(arrivedStr))
            foreach (string s in arrivedStr.Split(','))
                if (int.TryParse(s, out int idx)) arrivedStars.Add(idx);
    }

    /// <summary>
    /// 첫 인트로 완료 후 저장.
    /// </summary>
    public void MarkIntroSeen()
    {
        hasSeenIntro = true;
        SaveGame();
    }

    void OnApplicationQuit() => SaveGame();
    void OnApplicationPause(bool pause) { if (pause) SaveGame(); }

    // ============ UTILITY ============

    public static string FormatNumber(double n)
    {
        if (n >= 1e12) return (n / 1e12).ToString("F2") + "T";
        if (n >= 1e9) return (n / 1e9).ToString("F2") + "B";
        if (n >= 1e6) return (n / 1e6).ToString("F2") + "M";
        if (n >= 1e3) return (n / 1e3).ToString("F1") + "K";
        return Math.Floor(n).ToString();
    }

    public static string FormatCredits(double n)
    {
        if (n < 0) n = 0;
        return Math.Floor(n).ToString("N0");
    }

    public static string FormatSpeed(double kmPerSec)
    {
        if (kmPerSec < 0) kmPerSec = 0;

        bool isKo = Loc.Get("hud_heading_to").Contains("향하는중");

        if (isKo)
        {
            if (kmPerSec < 10000) return kmPerSec.ToString("F0") + " km/s";
            if (kmPerSec < 100000000) return (kmPerSec / 10000).ToString("F0") + "만 km/s";
            if (kmPerSec < 1000000000000) return (kmPerSec / 100000000).ToString("F1") + "억 km/s";
            return (kmPerSec / 1000000000000).ToString("F2") + "조 km/s";
        }
        else
        {
            if (kmPerSec < 1000) return kmPerSec.ToString("F0") + " km/s";
            if (kmPerSec < 1000000) return (kmPerSec / 1000).ToString("F1") + "K km/s";
            if (kmPerSec < 1000000000) return (kmPerSec / 1000000).ToString("F1") + "M km/s";
            return (kmPerSec / 1000000000).ToString("F2") + "B km/s";
        }
    }

    public static string FormatTime(double sec)
    {
        if (sec < 60) return Loc.Get("time_sec", $"{sec:F0}");
        if (sec < 3600) return Loc.Get("time_min", $"{sec / 60:F0}");
        if (sec < 86400) return Loc.Get("time_hour", $"{sec / 3600:F1}");
        return Loc.Get("time_day", $"{sec / 86400:F1}");
    }
}