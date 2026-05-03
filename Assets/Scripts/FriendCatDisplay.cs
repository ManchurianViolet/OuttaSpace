using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 근처 친구의 고양이를 화면에 표시.
/// 룰: 같은 구간 + 현재 별 거리의 절반 이내 (FriendSyncManager.nearbyDistanceRatio).
/// 최대 5명까지 표시 (초과 시 거리차 가장 작은 5명).
/// X는 거리차로, Y는 5개 슬롯 중 하나 (충돌 없이 배정).
/// 한번 받은 슬롯은 화면 떠날 때까지 유지.
/// 
/// 빈 GameObject에 붙이기. Inspector에서 playerShip 연결 필수.
/// </summary>
public class FriendCatDisplay : MonoBehaviour
{
    public const int MAX_VISIBLE = 5;

    [Header("References")]
    public Transform playerShip;          // 본인 ship Transform

    [Header("Display")]
    public float maxVisualDistance = 6f;  // 화면상 최대 X 오프셋 (월드 유닛)
    public float nameYOffset = 0.8f;      // 이름 라벨 위치 (고양이 위)
    public int friendSortingOrder = 9;    // 본인(10)보다 뒤
    public float updateInterval = 0.5f;   // 친구 목록 갱신 주기 (초)

    [Header("Y Slots (5 slots)")]
    [Tooltip("5개 Y 슬롯의 월드 Y 오프셋. 위에서 아래 순서.")]
    public float[] ySlotOffsets = new float[] { 1.2f, 0.6f, 0.0f, -0.6f, -1.2f };

    [Header("Friend Cat Appearance")]
    [Tooltip("본인 ship의 scale에 곱할 비율. 본인보다 살짝 작게 보이려면 0.85~0.95.")]
    public float friendCatScaleRatio = 0.9f;
    [Tooltip("화면에 들어왔을 때 최대 알파")]
    public float friendCatMaxAlpha = 0.85f;
    [Tooltip("화면 끝(임계 거리)에서의 최소 알파")]
    public float friendCatMinAlpha = 0.35f;

    [Header("Smoothing")]
    public float lerpSpeed = 5f;
    [Tooltip("친구 데이터가 끊겨도 이 시간 동안 페이드아웃하며 유지 (펄스 방지)")]
    public float graceTime = 3f;

    private Dictionary<ulong, FriendCatObject> activeFriendCats = new Dictionary<ulong, FriendCatObject>();
    private float updateTimer;

    // 슬롯 점유: index = 슬롯 번호, value = 점유 중인 steamId (0 = 비어있음)
    private ulong[] slotOwners;

    class FriendCatObject
    {
        public GameObject root;
        public SpriteRenderer catRenderer;
        public TextMeshPro nameLabel;
        public float targetX;
        public float targetAlpha;
        public int slotIndex;        // 점유 중인 Y 슬롯
        public float lastSeenTime;
        public bool seenThisTick;
        public int cachedCatId = -1;
    }

    void Awake()
    {
        slotOwners = new ulong[MAX_VISIBLE];
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;

        if (FriendSyncManager.Instance == null) return;
        if (GameManager.Instance == null || GameManager.Instance.isDocked)
        {
            HideAll();
            return;
        }

        foreach (var kvp in activeFriendCats)
            kvp.Value.seenThisTick = false;

        // 근처 친구 가져와서 거리차 작은 순으로 5명만
        List<FriendData> nearby = FriendSyncManager.Instance.GetNearbyFriends();
        double myDistance = GameManager.Instance.distance;
        nearby.Sort((a, b) =>
        {
            double da = System.Math.Abs(a.distanceKM - myDistance);
            double db = System.Math.Abs(b.distanceKM - myDistance);
            return da.CompareTo(db);
        });
        if (nearby.Count > MAX_VISIBLE)
            nearby = nearby.GetRange(0, MAX_VISIBLE);

        double thresholdKM = FriendSyncManager.Instance.GetNearbyThresholdKM();
        if (thresholdKM <= 0) thresholdKM = 1;

        foreach (FriendData fd in nearby)
        {
            // 거리차 → 화면 X (양수=앞=오른쪽, 음수=뒤=왼쪽)
            double distDiff = fd.distanceKM - myDistance;
            float normalized = (float)(distDiff / thresholdKM); // -1 ~ 1
            float targetX = Mathf.Clamp(normalized * maxVisualDistance, -maxVisualDistance, maxVisualDistance);

            float distRatio = Mathf.Clamp01(Mathf.Abs(normalized));
            float alpha = Mathf.Lerp(friendCatMaxAlpha, friendCatMinAlpha, distRatio);

            // 신규 친구
            if (!activeFriendCats.ContainsKey(fd.steamId))
            {
                int slot = AssignSlot(fd.steamId);
                if (slot < 0) continue; // 이론상 안 발생 (5명 컷이라)
                CreateFriendCat(fd, slot);
            }

            FriendCatObject fco = activeFriendCats[fd.steamId];

            if (fco.cachedCatId != fd.catType)
                ApplyCatSkin(fco, fd.catType);

            fco.targetX = targetX;
            fco.targetAlpha = alpha;
            fco.seenThisTick = true;
            fco.lastSeenTime = Time.time;

            if (fco.nameLabel != null)
                fco.nameLabel.text = fd.friendName;
        }

        // graceTime 지나면 제거 + 슬롯 반납
        List<ulong> toRemove = new List<ulong>();
        foreach (var kvp in activeFriendCats)
        {
            if (!kvp.Value.seenThisTick)
            {
                kvp.Value.targetAlpha = 0f;
                if (Time.time - kvp.Value.lastSeenTime > graceTime)
                    toRemove.Add(kvp.Key);
            }
        }
        foreach (ulong id in toRemove)
        {
            ReleaseSlot(id);
            if (activeFriendCats[id].root != null)
                Destroy(activeFriendCats[id].root);
            activeFriendCats.Remove(id);
        }
    }

    void LateUpdate()
    {
        if (playerShip == null) return;

        Vector3 baseScale = playerShip.lossyScale * friendCatScaleRatio;

        foreach (var kvp in activeFriendCats)
        {
            FriendCatObject fco = kvp.Value;
            if (fco.root == null) continue;

            float yOffset = GetSlotY(fco.slotIndex);
            Vector3 targetPos = playerShip.position + new Vector3(fco.targetX, yOffset, 0);
            fco.root.transform.position = Vector3.Lerp(
                fco.root.transform.position, targetPos, Time.deltaTime * lerpSpeed);

            fco.root.transform.localScale = baseScale;

            if (fco.catRenderer != null)
            {
                Color c = fco.catRenderer.color;
                c.a = Mathf.Lerp(c.a, fco.targetAlpha, Time.deltaTime * lerpSpeed);
                fco.catRenderer.color = c;
            }
            if (fco.nameLabel != null)
            {
                Color nc = fco.nameLabel.color;
                nc.a = Mathf.Lerp(nc.a, fco.targetAlpha * 0.95f, Time.deltaTime * lerpSpeed);
                fco.nameLabel.color = nc;
            }
        }
    }

    // ============ 슬롯 관리 ============

    /// <summary>
    /// 비어있는 슬롯 중 하나를 친구에게 할당.
    /// steamId 해시로 시작 인덱스 결정 → 같은 친구는 항상 같은 순서로 검사.
    /// 꽉 차있으면 -1 반환.
    /// </summary>
    int AssignSlot(ulong steamId)
    {
        int startIdx = (int)(steamId % (ulong)MAX_VISIBLE);
        for (int i = 0; i < MAX_VISIBLE; i++)
        {
            int idx = (startIdx + i) % MAX_VISIBLE;
            if (slotOwners[idx] == 0)
            {
                slotOwners[idx] = steamId;
                return idx;
            }
        }
        return -1;
    }

    void ReleaseSlot(ulong steamId)
    {
        for (int i = 0; i < MAX_VISIBLE; i++)
        {
            if (slotOwners[i] == steamId)
            {
                slotOwners[i] = 0;
                break;
            }
        }
    }

    float GetSlotY(int slotIndex)
    {
        if (ySlotOffsets == null || ySlotOffsets.Length == 0) return 0;
        if (slotIndex < 0 || slotIndex >= ySlotOffsets.Length) return 0;
        return ySlotOffsets[slotIndex];
    }

    // ============ 생성 ============

    void CreateFriendCat(FriendData fd, int slotIndex)
    {
        GameObject root = new GameObject($"FriendCat_{fd.friendName}");
        root.transform.SetParent(transform, worldPositionStays: false);

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = friendSortingOrder;
        sr.color = new Color(1f, 1f, 1f, 0f);

        // 이름 라벨 (초록)
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(root.transform, worldPositionStays: false);
        labelObj.transform.localPosition = new Vector3(0, nameYOffset, 0);

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = fd.friendName;
        tmp.fontSize = 2;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.36f, 0.79f, 0.36f, 0f);
        tmp.sortingOrder = friendSortingOrder + 1;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(4f, 1f);

        FriendCatObject fco = new FriendCatObject
        {
            root = root,
            catRenderer = sr,
            nameLabel = tmp,
            targetX = 0,
            targetAlpha = friendCatMaxAlpha,
            slotIndex = slotIndex,
            lastSeenTime = Time.time,
            seenThisTick = true,
            cachedCatId = -1,
        };

        activeFriendCats[fd.steamId] = fco;
        ApplyCatSkin(fco, fd.catType);
    }

    void ApplyCatSkin(FriendCatObject fco, int catId)
    {
        if (fco.catRenderer == null) return;

        Sprite sprite = null;
        if (CatDatabase.Instance != null)
        {
            int safeId = Mathf.Clamp(catId, 0, CatDatabase.TOTAL_COUNT - 1);
            CatData cd = CatDatabase.Instance.Get(safeId);
            if (cd != null) sprite = cd.sprite;
        }

        if (sprite == null && playerShip != null)
        {
            SpriteRenderer playerSR = playerShip.GetComponent<SpriteRenderer>();
            if (playerSR != null) sprite = playerSR.sprite;
        }

        fco.catRenderer.sprite = sprite;
        fco.cachedCatId = catId;
    }

    void HideAll()
    {
        foreach (var kvp in activeFriendCats)
        {
            if (kvp.Value.root != null)
                Destroy(kvp.Value.root);
        }
        activeFriendCats.Clear();
        for (int i = 0; i < slotOwners.Length; i++)
            slotOwners[i] = 0;
    }

    void OnDestroy()
    {
        foreach (var kvp in activeFriendCats)
        {
            if (kvp.Value.root != null)
                Destroy(kvp.Value.root);
        }
        activeFriendCats.Clear();
    }
}