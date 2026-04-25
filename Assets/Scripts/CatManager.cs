using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 보유 고양이 관리 + 현재 사용 중인 고양이 + Ship 스프라이트 교체.
/// 빈 GameObject에 붙이고 shipRenderer를 Inspector에서 연결.
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
        // 첫 실행이면 기본 고양이 지급
        if (ownedCats.Count == 0)
        {
            ownedCats.Add(CatDatabase.STARTER_CAT_ID);
            currentCatId = CatDatabase.STARTER_CAT_ID;
            Save();
        }
        ApplyCurrentCat();
    }

    // ============ 소유 확인 ============

    public bool IsOwned(int id) => ownedCats.Contains(id);

    public int OwnedCount => ownedCats.Count;

    // ============ 고양이 교체 ============

    public void SetCurrentCat(int id)
    {
        if (!IsOwned(id)) return;
        if (currentCatId == id) return;

        currentCatId = id;
        ApplyCurrentCat();
        OnCatChanged?.Invoke(id);
        Save();
    }

    public void ApplyCurrentCat()
    {
        if (shipRenderer == null) return;
        if (CatDatabase.Instance == null) return;

        CatData data = CatDatabase.Instance.Get(currentCatId);
        if (data != null && data.sprite != null)
            shipRenderer.sprite = data.sprite;
    }

    // ============ 가챠 결과 처리 ============

    /// <summary>
    /// 가챠 결과 처리. 새 고양이면 추가, 중복이면 false 반환.
    /// </summary>
    public bool AddFromGacha(int id)
    {
        if (IsOwned(id)) return false;

        ownedCats.Add(id);
        OnCatsChanged?.Invoke();
        Save();
        return true;
    }

    // ============ 저장/로드 ============

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
