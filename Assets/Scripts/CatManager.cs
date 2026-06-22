using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 보유 고양이 관리 + 현재 사용 중인 고양이 + Ship 스프라이트 교체.
/// </summary>
public class CatManager : MonoBehaviour
{
    public static CatManager Instance { get; private set; }

    [Header("Ship Reference")]
    public SpriteRenderer shipRenderer;

    [Header("State")]
    public HashSet<int> ownedCats = new HashSet<int>();
    public int currentCatId = 0;

    public event Action OnCatsChanged;
    public event Action<int> OnCatChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    void Start()
    {
        if (ownedCats.Count == 0)
        {
            ownedCats.Add(CatDatabase.STARTER_CAT_ID);
            currentCatId = CatDatabase.STARTER_CAT_ID;
            Save();
        }
        ApplyCurrentCat();
    }

    public bool IsOwned(int id) => ownedCats.Contains(id);
    public int OwnedCount => ownedCats.Count;

    public void SetCurrentCat(int id)
    {
        if (!IsOwned(id)) return;
        if (currentCatId == id) return;

        currentCatId = id;
        ApplyCurrentCat();
        OnCatChanged?.Invoke(id);
        Save();

        // 속도가 등급 따라 바뀌므로 stats 갱신 알림
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyStatsChanged();
    }

    /// <summary>
    /// Ship 스프라이트 + 엔진 불꽃 색 둘 다 갱신.
    /// 정박 중에도 즉시 보이도록 ShipController.InvalidateFlameColor() 호출.
    /// </summary>
    public void ApplyCurrentCat()
    {
        if (CatDatabase.Instance == null) return;

        CatData data = CatDatabase.Instance.Get(currentCatId);

        // Ship 스프라이트
        if (shipRenderer != null && data != null && data.sprite != null)
            shipRenderer.sprite = data.sprite;

        // 엔진 불꽃 색 즉시 갱신 (정박 중에도)
        if (shipRenderer != null)
        {
            ShipController sc = shipRenderer.GetComponent<ShipController>();
            if (sc != null)
                sc.InvalidateFlameColor();
        }
    }

    public bool AddFromGacha(int id)
    {
        if (IsOwned(id)) return false;

        ownedCats.Add(id);
        OnCatsChanged?.Invoke();
        Save();
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.CheckCollection();
        return true;
    }

    void Save()
    {
        PlayerPrefs.SetString("OwnedCats", string.Join(",", ownedCats));
        PlayerPrefs.SetInt("CurrentCat", currentCatId);
        PlayerPrefs.Save();
    }

    void Load()
    {
        string str = PlayerPrefs.GetString("OwnedCats", "");
        ownedCats.Clear();
        if (!string.IsNullOrEmpty(str))
        {
            foreach (string s in str.Split(','))
                if (int.TryParse(s, out int id)) ownedCats.Add(id);
        }
        currentCatId = PlayerPrefs.GetInt("CurrentCat", CatDatabase.STARTER_CAT_ID);
    }
}
