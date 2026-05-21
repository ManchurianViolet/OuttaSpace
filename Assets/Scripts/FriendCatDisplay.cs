using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 근처 친구의 고양이를 화면에 표시.
/// 룰: 같은 구간 + 현재 별 거리의 절반 이내 (FriendSyncManager.nearbyDistanceRatio).
/// 최대 5명까지 표시 (초과 시 거리차 가장 작은 5명).
///
/// 슬롯마다 (X, Y) base 오프셋이 다르게 배정되어 같은 거리에 있어도 부채꼴로 흩어진다.
/// 친구마다 고유한 호버 phase/amplitude/speed로 본인과 다른 리듬으로 부유한다.
/// 각 친구 고양이 뒤에 자체 엔진 불꽃 파티클이 생성되며, 친구의 catType에 따라
/// 불꽃 색이 결정된다.
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
    public float[] ySlotOffsets = new float[] { 1.2f, 0.6f, 0.0f, -0.6f, -1.2f };
    public float[] xSlotOffsets = new float[] { 2.0f, 2.5f, 3.0f, 2.5f, 2.0f };

    [Header("Friend Cat Appearance")]
    public float friendCatScaleRatio = 0.9f;
    public float friendCatMaxAlpha = 0.85f;
    public float friendCatMinAlpha = 0.35f;

    [Header("Friend Bobbing (각자 고유 리듬)")]
    public Vector2 friendBobAmplitudeRange = new Vector2(0.10f, 0.18f);
    public Vector2 friendBobSpeedRange = new Vector2(0.9f, 1.5f);

    [Header("Engine Flame")]
    public Vector3 flameLocalPosition = new Vector3(-3.1f, -0.45f, 0);
    public int flameSortingOffset = -1;

    [Header("Name Label")]
    public float nameFontSize = 6f;
    public Vector2 nameLabelSize = new Vector2(8f, 2f);
    public bool counterScaleNameLabel = true;
    public Color nameColor = new Color(0.36f, 0.79f, 0.36f);

    [Header("Smoothing")]
    public float lerpSpeed = 5f;
    public float graceTime = 3f;

    private Dictionary<ulong, FriendCatObject> activeFriendCats = new Dictionary<ulong, FriendCatObject>();
    private float updateTimer;

    private ulong[] slotOwners;
    private ShipController playerShipController;

    // 디버그 로그 타이머 (2초마다 한 번)
    private float diagLogTimer;

    class FriendCatObject
    {
        public GameObject root;
        public SpriteRenderer catRenderer;
        public TextMeshPro nameLabel;
        public Transform nameLabelTransform;
        public ParticleSystem flame;
        public float targetX;
        public float targetAlpha;
        public int slotIndex;
        public float lastSeenTime;
        public bool seenThisTick;
        public int cachedCatId = -1; // 스킨/불꽃색 캐시
        public float bobPhaseOffset;
        public float bobAmplitude;
        public float bobSpeed;
    }

    void Awake()
    {
        slotOwners = new ulong[MAX_VISIBLE];
    }

    void Start()
    {
        if (playerShip != null)
            playerShipController = playerShip.GetComponent<ShipController>();
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

        // [DEBUG] 친구 진단 — 2초마다 한 번씩만 찍음 (스팸 방지)
        diagLogTimer += updateInterval;
        if (diagLogTimer >= 2f)
        {
            diagLogTimer = 0f;
            var allFriends = FriendSyncManager.Instance.GetAllLiveFriends();
            double thresh = FriendSyncManager.Instance.GetNearbyThresholdKM();
            string s = $"[Friends] myStar={GameManager.Instance.currentStarIndex} myDist={myDistance:F0} threshold={thresh:F0} totalLive={allFriends.Count} nearby={nearby.Count}";
            foreach (var fd in allFriends)
            {
                double diff = System.Math.Abs(fd.distanceKM - myDistance);
                s += $"\n  → {fd.friendName}: star={fd.currentStarIndex} dist={fd.distanceKM:F0} diff={diff:F0} docked={fd.isDocked}";
            }
            Debug.Log(s);
        }

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
                if (fco.flame != null && !fco.flame.isPlaying)
                    fco.flame.Play();
            }

            // 고양이 바뀌면 스킨 + 불꽃 색 둘 다 새로 적용
            if (fco.cachedCatId != fd.catType)
            {
                ApplyCatSkin(fco, fd.catType);
                ApplyFlameColor(fco, fd.catType);
                fco.cachedCatId = fd.catType;
            }

            // X 위치 계산
            // - 친구가 본인보다 앞서면 (deltaX > 0): 본인 오른쪽 + 슬롯 baseX
            // - 친구가 본인보다 뒤처지면 (deltaX < 0): 본인 왼쪽 + 슬롯 baseX (음수로 반전)
            // 이러면 본인 위치(0)를 중심으로 거리차에 따라 좌/우로 분산되고,
            // 동시에 슬롯 baseX로 여러 친구가 안 겹침
            float baseX = GetSlotX(fco.slotIndex);
            float deltaSign = deltaX >= 0 ? 1f : -1f;
            float targetX = Mathf.Clamp(baseX * deltaSign + deltaX, -maxVisualDistance, maxVisualDistance);

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

        Vector3 playerBase = playerShip.position;
        if (playerShipController != null)
        {
            float playerBob = Mathf.Sin(Time.time * playerShipController.bobSpeed)
                            * playerShipController.bobAmplitude;
            playerBase.y -= playerBob;
        }

        Vector3 baseScale = playerShip.lossyScale * friendCatScaleRatio;

        foreach (var kvp in activeFriendCats)
        {
            FriendCatObject fco = kvp.Value;
            if (fco.root == null) continue;
            if (!fco.root.activeSelf) continue;

            float yOffset = GetSlotY(fco.slotIndex);
            float friendBob = Mathf.Sin((Time.time + fco.bobPhaseOffset) * fco.bobSpeed)
                            * fco.bobAmplitude;

            Vector3 targetPos = playerBase + new Vector3(fco.targetX, yOffset + friendBob, 0);
            fco.root.transform.position = Vector3.Lerp(
                fco.root.transform.position, targetPos, Time.deltaTime * lerpSpeed);

            fco.root.transform.localScale = baseScale;

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

    // ============ 슬롯 ============

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

        ParticleSystem flame = CreateFlame(root.transform, sr.sortingOrder + flameSortingOffset);

        System.Random rng = new System.Random(fd.steamId.GetHashCode());
        float phase = (float)(rng.NextDouble() * 2.0 * System.Math.PI);
        float amp = Mathf.Lerp(friendBobAmplitudeRange.x, friendBobAmplitudeRange.y, (float)rng.NextDouble());
        float spd = Mathf.Lerp(friendBobSpeedRange.x, friendBobSpeedRange.y, (float)rng.NextDouble());

        FriendCatObject fco = new FriendCatObject
        {
            root = root,
            catRenderer = sr,
            nameLabel = tmp,
            nameLabelTransform = labelObj.transform,
            flame = flame,
            targetX = 0,
            targetAlpha = friendCatMaxAlpha,
            slotIndex = slotIndex,
            lastSeenTime = Time.time,
            seenThisTick = true,
            cachedCatId = -1,
            bobPhaseOffset = phase,
            bobAmplitude = amp,
            bobSpeed = spd,
        };

        activeFriendCats[fd.steamId] = fco;
        ApplyCatSkin(fco, fd.catType);
        ApplyFlameColor(fco, fd.catType);
        fco.cachedCatId = fd.catType;
    }

    /// <summary>
    /// 친구 고양이 뒤 엔진 불꽃 파티클 생성.
    /// 초기 색은 흰색이고, ApplyFlameColor에서 catType 기반으로 즉시 덮어쓴다.
    /// </summary>
    ParticleSystem CreateFlame(Transform parent, int sortingOrder)
    {
        GameObject flameObj = new GameObject("EngineFlame");
        flameObj.transform.SetParent(parent, worldPositionStays: false);
        flameObj.transform.localPosition = flameLocalPosition;

        ParticleSystem ps = flameObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 5f;
        main.startSize = 0.05f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(0, 0, 90);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = -3f;
        velocity.y = 0f;
        velocity.z = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        var renderer = flameObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        return ps;
    }

    /// <summary>
    /// 친구 고양이의 catType에 맞춰 불꽃 색 적용.
    /// CatDatabase의 헬퍼로 startColor + colorOverLifetime 그라데이션 생성.
    /// </summary>
    void ApplyFlameColor(FriendCatObject fco, int catId)
    {
        if (fco.flame == null) return;

        Color flameColor = CatDatabase.GetFlameColor(catId);
        var main = fco.flame.main;
        main.startColor = CatDatabase.BuildFlameStartColor(flameColor, 0.8f);

        var colorOverLife = fco.flame.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = new ParticleSystem.MinMaxGradient(
            CatDatabase.BuildFlameGradient(flameColor));
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
    }

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