using UnityEngine;
using System;
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

    [Header("Upgrade Levels")]
    public int speedLevel;
    public int boosterCapLevel;
    public int boosterSpdLevel;
    public int catSwapLevel;

    [Header("Settings")]
    public float tickRate = 0.05f;

    public event Action<StarData> OnStarArrived;
    public event Action OnStatsChanged;
    public event Action<string> OnNotification;

    private float tickTimer;
    private float saveTimer;

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
        // 부스터 연료 복원
        if (BoosterSystem.Instance != null)
            BoosterSystem.Instance.fuel = PlayerPrefs.GetFloat("BoosterFuel", 0f);
    }

    void Update()
    {
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
        credits += creditGain;
        totalCredits += creditGain;

        StarData current = StarDatabase.Stars[currentStarIndex];
        if (distance >= current.distanceKM && !arrivedStars.Contains(currentStarIndex))
            ArriveAtStar(currentStarIndex);

        OnStatsChanged?.Invoke();
    }

    void ArriveAtStar(int index)
    {
        StarData star = StarDatabase.Stars[index];
        arrivedStars.Add(index);
        credits += star.reward;
        totalCredits += star.reward;
        isDocked = true;

        OnStarArrived?.Invoke(star);
        OnNotification?.Invoke(Loc.Get("notif_arrived", Loc.Get(star.nameKey), star.reward));
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

    public double GetTotalSpeed()
    {
        double spd = 100;
        spd += GetUpgradeContribution(UpgradeType.Speed);
        return spd;
    }

    public double GetEffectiveSpeed()
    {
        double baseSpeed = GetTotalSpeed();
        float boostMult = 1f;
        if (BoosterSystem.Instance != null)
            boostMult = BoosterSystem.Instance.GetSpeedMultiplier();
        return baseSpeed * boostMult;
    }

    /// <summary>부스터 용량 배율 (1 + 업그레이드 기여)</summary>
    public float GetBoosterCapacityMultiplier()
    {
        return 1f + (float)GetUpgradeContribution(UpgradeType.BoosterCapacity);
    }

    /// <summary>부스터 속도 배율 (3 + 업그레이드 기여)</summary>
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
            UpgradeType.BoosterCapacity => boosterCapLevel,
            UpgradeType.BoosterSpeed => boosterSpdLevel,
            UpgradeType.CatSwap => catSwapLevel,
            _ => 0
        };
    }

    // ============ UPGRADES ============

    public double GetUpgradeCost(UpgradeType type)
    {
        UpgradeData data = UpgradeDatabase.Get(type);
        int level = GetUpgradeLevel(type);
        return Math.Floor(data.baseCost * Math.Pow(data.costMult, level));
    }

    public bool CanAfford(UpgradeType type) => credits >= GetUpgradeCost(type);

    public bool BuyUpgrade(UpgradeType type)
    {
        if (type == UpgradeType.CatSwap) return false; // 데모 비활성화

        double cost = GetUpgradeCost(type);
        if (credits < cost) return false;
        credits -= cost;
        switch (type)
        {
            case UpgradeType.Speed: speedLevel++; break;
            case UpgradeType.BoosterCapacity: boosterCapLevel++; break;
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
        PlayerPrefs.SetInt("BoosterCapLevel", boosterCapLevel);
        PlayerPrefs.SetInt("BoosterSpdLevel", boosterSpdLevel);
        PlayerPrefs.SetInt("CatSwapLevel", catSwapLevel);
        PlayerPrefs.SetInt("IsDocked", isDocked ? 1 : 0);
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
        boosterCapLevel = PlayerPrefs.GetInt("BoosterCapLevel", 0);
        boosterSpdLevel = PlayerPrefs.GetInt("BoosterSpdLevel", 0);
        catSwapLevel = PlayerPrefs.GetInt("CatSwapLevel", 0);
        isDocked = PlayerPrefs.GetInt("IsDocked", 0) == 1;

        string arrivedStr = PlayerPrefs.GetString("ArrivedStars", "");
        arrivedStars.Clear();
        if (!string.IsNullOrEmpty(arrivedStr))
            foreach (string s in arrivedStr.Split(','))
                if (int.TryParse(s, out int idx)) arrivedStars.Add(idx);
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

    public static string FormatSpeed(double kmPerSec)
    {
        double c = 299792.458;
        if (kmPerSec >= c) return Loc.Get("speed_c", (kmPerSec / c).ToString("F2"));
        if (kmPerSec >= c * 0.01) return Loc.Get("speed_pctc", (kmPerSec / c * 100).ToString("F1"));
        if (kmPerSec >= 1000) return Loc.Get("speed_tkms", (kmPerSec / 1000).ToString("F1"));
        return Loc.Get("speed_kms", kmPerSec.ToString("F0"));
    }

    public static string FormatTime(double sec)
    {
        if (sec < 60) return Loc.Get("time_sec", $"{sec:F0}");
        if (sec < 3600) return Loc.Get("time_min", $"{sec / 60:F0}");
        if (sec < 86400) return Loc.Get("time_hour", $"{sec / 3600:F1}");
        return Loc.Get("time_day", $"{sec / 86400:F1}");
    }
}
