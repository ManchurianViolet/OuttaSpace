using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 근처 친구의 고양이를 화면에 표시.
/// 룰: 같은 구간 + 현재 별 거리의 절반 이내 (FriendSyncManager.nearbyDistanceRatio).
/// 최대 5명까지 표시 (초과 시 거리차 가장 작은 5명).
///
/// 슬롯마다 (X, Y) base 오프셋이 다르게 배정되어 같은 거리에 있어도 부채꼴로 흩어진다.
/// 거리 차이는 그 위에 더해져서 친구가 앞서가면 우측, 뒤처지면 본인 옆쪽으로.
/// </summary>
public class FriendCatDisplay : MonoBehaviour
{
    public const int MAX_VISIBLE = 5;

    [Header("References")]
    public Transform playerShip;

    [Header("Display")]
    public float maxVisualDistance = 6f;
    public float nameYOffset = 0.8f;
    public int friendSortingOrder = 9;
    public float updateInterval = 0.5f;

    [Header("Slot Offsets (5 slots, base position relative to player)")]
    [Tooltip("슬롯마다 Y 오프셋. 같은 거리 친구들이 위/아래로 흩어짐.")]
    public float[] ySlotOffsets = new float[] { 1.2f, 0.6f, 0.0f, -0.6f, -1.2f };

    [Tooltip("슬롯마다 X 오프셋. 본인과 같은 거리여도 X로 분산되어 겹치지 않음. 부채꼴 형태.")]
    public float[] xSlotOffsets = new float[] { 2.0f, 2.5f, 3.0f, 2.5f, 2.0f };

    [Header("Friend Cat Appearance")]
    [Tooltip("본인 ship의 scale에 곱할 비율.")]
    public float friendCatScaleRatio = 0.9f;
    public float friendCatMaxAlpha = 0.85f;
    public float friendCatMinAlpha = 0.35f;

    [Header("Name Label")]
    [Tooltip("이름 라벨 폰트 크기 (월드 단위). 기본 6.")]
    public float nameFontSize = 6f;
    [Tooltip("이름 라벨 RectTransform 크기 (가로/세로).")]
    public Vector2 nameLabelSize = new Vector2(8f, 2f);
    [Tooltip("이름 라벨이 친구 스케일 영향 받지 않도록 보정 (true 권장).")]
    public bool counterScaleNameLabel = true;
    public Color nameColor = new Color(0.36f, 0.79f, 0.36f); // 초록

    [Header("Smoothing")]
    public float lerpSpeed = 5f;
    public float graceTime = 3f;

    private Dictionary<ulong, FriendCatObject> activeFriendCats = new Dictionary<ulong, FriendCatObject>();
    private float updateTimer;

    private ulong[] slotOwners;

    class FriendCatObject
    {
        public GameObject root;
        public SpriteRenderer catRenderer;
        public TextMeshPro nameLabel;
        public Transform nameLabelTransform;
        public float targetX;
        public float targetAlpha;
        public int slotIndex;
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
            HideAllVisuals();
            return;
        }

        foreach (var kvp in activeFriendCats)
            kvp.Value.seenThisTick = false;

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
            double distDiff = fd.distanceKM - myDistance;
            float normalized = (float)(distDiff / thresholdKM);
            float deltaX = normalized * maxVisualDistance;

            float distRatio = Mathf.Clamp01(Mathf.Abs(normalized));
            float alpha = Mathf.Lerp(friendCatMaxAlpha, friendCatMinAlpha, distRatio);

            if (!activeFriendCats.ContainsKey(fd.steamId))
            {
                int slot = AssignSlot(fd.steamId);
                if (slot < 0) continue;
                CreateFriendCat(fd, slot);
            }

            FriendCatObject fco = activeFriendCats[fd.steamId];

            // 정박 중 SetActive(false)됐던 객체 재활성
            if (fco.root != null && !fco.root.activeSelf)
            {
                fco.root.SetActive(true);
                if (fco.catRenderer != null)
                {
                    Color c = fco.catRenderer.color;
                    c.a = 0f;
                    fco.catRenderer.color = c;
                }
                if (fco.nameLabel != null)
                {
                    Color nc = fco.nameLabel.color;
                    nc.a = 0f;
                    fco.nameLabel.color = nc;
                }
            }

            if (fco.cachedCatId != fd.catType)
                ApplyCatSkin(fco, fd.catType);

            // 슬롯 baseX + 거리차 deltaX로 X 결정
            // → 같은 거리(deltaX=0)여도 슬롯마다 X가 달라 친구들이 흩어짐
            float baseX = GetSlotX(fco.slotIndex);
            float targetX = Mathf.Clamp(baseX + deltaX, -maxVisualDistance, maxVisualDistance);

            fco.targetX = targetX;
            fco.targetAlpha = alpha;
            fco.seenThisTick = true;
            fco.lastSeenTime = Time.time;

            if (fco.nameLabel != null)
                fco.nameLabel.text = fd.friendName;
        }

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
            if (!fco.root.activeSelf) continue;

            float yOffset = GetSlotY(fco.slotIndex);
            Vector3 targetPos = playerShip.position + new Vector3(fco.targetX, yOffset, 0);
            fco.root.transform.position = Vector3.Lerp(
                fco.root.transform.position, targetPos, Time.deltaTime * lerpSpeed);

            fco.root.transform.localScale = baseScale;

            // 이름 라벨이 친구 스케일 영향 안 받게 보정
            if (counterScaleNameLabel && fco.nameLabelTransform != null)
            {
                float sx = baseScale.x != 0 ? 1f / baseScale.x : 1f;
                float sy = baseScale.y != 0 ? 1f / baseScale.y : 1f;
                fco.nameLabelTransform.localScale = new Vector3(sx, sy, 1f);
                fco.nameLabelTransform.localPosition = new Vector3(0, nameYOffset * sy, 0);
            }

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

    float GetSlotX(int slotIndex)
    {
        if (xSlotOffsets == null || xSlotOffsets.Length == 0) return 0;
        if (slotIndex < 0 || slotIndex >= xSlotOffsets.Length) return 0;
        return xSlotOffsets[slotIndex];
    }

    // ============ 생성 ============

    void CreateFriendCat(FriendData fd, int slotIndex)
    {
        GameObject root = new GameObject($"FriendCat_{fd.friendName}");
        root.transform.SetParent(transform, worldPositionStays: false);

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = friendSortingOrder;
        sr.color = new Color(1f, 1f, 1f, 0f);

        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(root.transform, worldPositionStays: false);
        labelObj.transform.localPosition = new Vector3(0, nameYOffset, 0);

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = fd.friendName;
        tmp.fontSize = nameFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        Color startColor = nameColor;
        startColor.a = 0f;
        tmp.color = startColor;
        tmp.sortingOrder = friendSortingOrder + 1;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = nameLabelSize;

        FriendCatObject fco = new FriendCatObject
        {
            root = root,
            catRenderer = sr,
            nameLabel = tmp,
            nameLabelTransform = labelObj.transform,
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

    /// <summary>
    /// 정박 중에는 친구 GameObject를 Destroy하지 않고 비활성화만.
    /// 항해 복귀 시 재활성 가능.
    /// </summary>
    void HideAllVisuals()
    {
        foreach (var kvp in activeFriendCats)
        {
            if (kvp.Value.root != null && kvp.Value.root.activeSelf)
                kvp.Value.root.SetActive(false);
        }
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