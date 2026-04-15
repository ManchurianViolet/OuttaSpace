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

    public double GetTotalSpeed()
    {
        double spd = 3000;
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

    public float GetBoosterCapacityMultiplier()
    {
        return 1f + (float)GetUpgradeContribution(UpgradeType.BoosterCapacity);
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
        if (type == UpgradeType.CatSwap) return false;

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