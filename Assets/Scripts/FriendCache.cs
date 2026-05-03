using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 한 번이라도 같이 게임을 만난 친구들을 영구 저장.
/// PlayerPrefs 직렬화 형식: "id|cat|star|dist|docked;id|cat|star|dist|docked;..."
/// </summary>
public static class FriendCache
{
    private const string PREFS_KEY = "FriendCache_v1";

    public class Entry
    {
        public ulong steamId;
        public int currentCatId;
        public int lastStarIndex;
        public double lastDistanceKM;
        public bool wasDocked;
    }

    private static Dictionary<ulong, Entry> cache;

    static void EnsureLoaded()
    {
        if (cache != null) return;
        cache = new Dictionary<ulong, Entry>();

        string raw = PlayerPrefs.GetString(PREFS_KEY, "");
        if (string.IsNullOrEmpty(raw)) return;

        foreach (var part in raw.Split(';'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            var t = part.Split('|');
            if (t.Length < 5) continue;

            try
            {
                var e = new Entry
                {
                    steamId = ulong.Parse(t[0]),
                    currentCatId = int.Parse(t[1]),
                    lastStarIndex = int.Parse(t[2]),
                    lastDistanceKM = double.Parse(t[3], CultureInfo.InvariantCulture),
                    wasDocked = t[4] == "1",
                };
                cache[e.steamId] = e;
            }
            catch { }
        }
    }

    public static void Update(ulong steamId, int catId, int starIndex, double distance, bool docked)
    {
        EnsureLoaded();
        cache[steamId] = new Entry
        {
            steamId = steamId,
            currentCatId = catId,
            lastStarIndex = starIndex,
            lastDistanceKM = distance,
            wasDocked = docked,
        };
        Save();
    }

    public static IEnumerable<Entry> GetAll()
    {
        EnsureLoaded();
        return cache.Values;
    }

    public static Entry Get(ulong steamId)
    {
        EnsureLoaded();
        return cache.TryGetValue(steamId, out var e) ? e : null;
    }

    static void Save()
    {
        var sb = new StringBuilder();
        foreach (var e in cache.Values)
        {
            sb.Append(e.steamId).Append('|')
              .Append(e.currentCatId).Append('|')
              .Append(e.lastStarIndex).Append('|')
              .Append(e.lastDistanceKM.ToString("R", CultureInfo.InvariantCulture)).Append('|')
              .Append(e.wasDocked ? "1" : "0").Append(';');
        }
        PlayerPrefs.SetString(PREFS_KEY, sb.ToString());
    }
}
