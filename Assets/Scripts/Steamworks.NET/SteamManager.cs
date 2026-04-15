using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

/// <summary>
/// Steamworks 초기화 및 콜백 관리.
/// 빌드 시 steam_appid.txt를 자동으로 빌드 폴더에 복사.
/// AppID: 4616690
/// </summary>
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    public const uint APP_ID = 4616690;

#if !DISABLESTEAMWORKS
    private bool steamInitialized;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test())
        {
            Debug.LogError("[Steam] Packsize test failed.");
            return;
        }

        if (!DllCheck.Test())
        {
            Debug.LogError("[Steam] DllCheck test failed. steam_api64.dll missing.");
            return;
        }
        try
        {
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(APP_ID)))
            {
                Debug.Log("[Steam] Restarting through Steam client.");
                Application.Quit();
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steam] steam_api64.dll not found: " + e.Message);
            Application.Quit();
            return;
        }
        try
        {
            steamInitialized = SteamAPI.Init();

            if (!steamInitialized)
            {
                Debug.LogWarning("[Steam] SteamAPI.Init() failed. Is Steam running?");
                return;
            }

            Initialized = true;
            string userName = SteamFriends.GetPersonaName();
            Debug.Log($"[Steam] Initialized. User: {userName} (ID: {SteamUser.GetSteamID()})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Steam] Init exception: {e.Message}");
            steamInitialized = false;
        }
    }

    void Update()
    {
        if (steamInitialized)
            SteamAPI.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (steamInitialized)
        {
            SteamAPI.Shutdown();
            steamInitialized = false;
            Initialized = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#else
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Steam] Steamworks disabled (DISABLESTEAMWORKS defined)");
    }
#endif
}

// ============ 빌드 시 steam_appid.txt 자동 복사 ============
#if UNITY_EDITOR
public class SteamAppIdPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        string buildFolder = System.IO.Path.GetDirectoryName(report.summary.outputPath);
        string appIdPath = System.IO.Path.Combine(buildFolder, "steam_appid.txt");

        System.IO.File.WriteAllText(appIdPath, SteamManager.APP_ID.ToString());
        Debug.Log($"[Steam] steam_appid.txt written to {appIdPath} (AppID: {SteamManager.APP_ID})");
    }
}
#endif