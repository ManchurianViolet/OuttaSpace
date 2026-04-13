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
    public int engineLevel;
    public int fuelLevel;
    public int navLevel;
    public int warpLevel;

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
        // 오프라인 진행 없음 — 종료 시점 그대로 복원
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

        // 스페이스바로 출발 (정박 UI 완성 전 임시)
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

        // 30초마다 자동 저장
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
        if (!hasFocus) SaveGame(); // 포커스 잃으면 저장
    }

    void ProcessTick()
    {
        if (isDocked) return;

        double spd = GetTotalSpeed();
        double distGain = spd * tickRate;
        double creditGain = spd * 0.00001 * tickRate;

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
        OnNotification?.Invoke($"★ {star.name} 도착! +{star.reward} CR");
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

    // ============ SPEED (km/s) ============

    public double GetTotalSpeed()
    {
        double spd = 5000;
        spd += GetUpgradeContribution(UpgradeType.Engine);
        spd += GetUpgradeContribution(UpgradeType.Fuel);
        spd += GetUpgradeContribution(UpgradeType.Nav);
        spd += GetUpgradeContribution(UpgradeType.Warp);
        return spd;
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
            UpgradeType.Engine => engineLevel,
            UpgradeType.Fuel => fuelLevel,
            UpgradeType.Nav => navLevel,
            UpgradeType.Warp => warpLevel,
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
        double cost = GetUpgradeCost(type);
        if (credits < cost) return false;
        credits -= cost;
        switch (type)
        {
            case UpgradeType.Engine: engineLevel++; break;
            case UpgradeType.Fuel: fuelLevel++; break;
            case UpgradeType.Nav: navLevel++; break;
            case UpgradeType.Warp: warpLevel++; break;
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
        PlayerPrefs.SetInt("EngineLevel", engineLevel);
        PlayerPrefs.SetInt("FuelLevel", fuelLevel);
        PlayerPrefs.SetInt("NavLevel", navLevel);
        PlayerPrefs.SetInt("WarpLevel", warpLevel);
        PlayerPrefs.SetInt("IsDocked", isDocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadGame()
    {
        credits = double.Parse(PlayerPrefs.GetString("Credits", "0"));
        totalCredits = double.Parse(PlayerPrefs.GetString("TotalCredits", "0"));
        distance = double.Parse(PlayerPrefs.GetString("Distance", "0"));
        totalDistance = double.Parse(PlayerPrefs.GetString("TotalDistance", "0"));
        currentStarIndex = PlayerPrefs.GetInt("CurrentStar", 0);
        engineLevel = PlayerPrefs.GetInt("EngineLevel", 0);
        fuelLevel = PlayerPrefs.GetInt("FuelLevel", 0);
        navLevel = PlayerPrefs.GetInt("NavLevel", 0);
        warpLevel = PlayerPrefs.GetInt("WarpLevel", 0);
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
        if (kmPerSec >= c) return (kmPerSec / c).ToString("F2") + "c";
        if (kmPerSec >= c * 0.01) return (kmPerSec / c * 100).ToString("F1") + "% c";
        if (kmPerSec >= 1000) return (kmPerSec / 1000).ToString("F1") + "천 km/s";
        return kmPerSec.ToString("F0") + " km/s";
    }
}
