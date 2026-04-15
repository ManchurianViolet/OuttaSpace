using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 근처 친구의 고양이를 화면에 표시.
/// 거리 차이에 따라 앞뒤로 배치, 속도 차이에 따라 이동.
/// 이름 라벨도 표시.
/// 
/// 빈 GameObject에 붙이기.
/// </summary>
public class FriendCatDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform playerShip;      // 내 고양이 (위치 기준)

    [Header("Display Settings")]
    public float maxVisualDistance = 6f;   // 화면상 최대 X 오프셋 (유닛)
    public double maxGameDistance = 50000; // 이 거리(km)가 maxVisualDistance에 매핑
    public float yOffset = 0.5f;          // 플레이어 위아래로 살짝 비껴서 표시
    public float nameYOffset = 0.8f;      // 이름 라벨 위치 (고양이 위)
    public int friendSortingOrder = 9;    // 플레이어(10)보다 뒤에
    public float updateInterval = 0.5f;   // 표시 갱신 주기

    [Header("Friend Cat Appearance")]
    public float friendCatScale = 0.8f;   // 플레이어보다 약간 작게
    public float friendCatAlpha = 0.7f;   // 약간 투명

    // 활성 친구 고양이 오브젝트
    private Dictionary<ulong, FriendCatObject> activeFriendCats = new Dictionary<ulong, FriendCatObject>();
    private float updateTimer;

    class FriendCatObject
    {
        public GameObject root;
        public SpriteRenderer catRenderer;
        public TextMeshPro nameLabel;
        public float targetX;
        public float targetAlpha;
        public bool alive;           // 이번 프레임에 갱신됐는지
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;

        if (FriendSyncManager.Instance == null) return;
        if (GameManager.Instance == null || GameManager.Instance.isDocked) 
        {
            // 정박 중이면 모든 친구 고양이 숨기기
            HideAll();
            return;
        }

        // 모든 친구 alive = false
        foreach (var kvp in activeFriendCats)
            kvp.Value.alive = false;

        // 근처 친구 가져오기
        List<FriendData> nearby = FriendSyncManager.Instance.GetNearbyFriends();

        double myDistance = GameManager.Instance.distance;

        foreach (FriendData fd in nearby)
        {
            // 거리 차이 → 화면 X 위치
            double distDiff = fd.distanceKM - myDistance;
            // 양수 = 앞에 있음 (오른쪽), 음수 = 뒤에 있음 (왼쪽)
            float normalizedDist = (float)(distDiff / maxGameDistance);
            float targetX = Mathf.Clamp(normalizedDist * maxVisualDistance, -maxVisualDistance, maxVisualDistance);

            // 가까울수록 선명, 멀수록 투명
            float distRatio = Mathf.Clamp01((float)(System.Math.Abs(distDiff) / maxGameDistance));
            float alpha = Mathf.Lerp(friendCatAlpha, 0.2f, distRatio);

            // 이미 존재하면 업데이트, 없으면 생성
            if (!activeFriendCats.ContainsKey(fd.steamId))
            {
                CreateFriendCat(fd);
            }

            FriendCatObject fco = activeFriendCats[fd.steamId];
            fco.targetX = targetX;
            fco.targetAlpha = alpha;
            fco.alive = true;

            // 이름 업데이트
            if (fco.nameLabel != null)
                fco.nameLabel.text = fd.friendName;
        }

        // alive가 false인 친구 제거
        List<ulong> toRemove = new List<ulong>();
        foreach (var kvp in activeFriendCats)
        {
            if (!kvp.Value.alive)
                toRemove.Add(kvp.Key);
        }
        foreach (ulong id in toRemove)
        {
            Destroy(activeFriendCats[id].root);
            activeFriendCats.Remove(id);
        }
    }

    void LateUpdate()
    {
        if (playerShip == null) return;

        // 부드러운 위치/투명도 보간
        foreach (var kvp in activeFriendCats)
        {
            FriendCatObject fco = kvp.Value;
            if (fco.root == null) continue;

            // 플레이어 기준 상대 위치
            Vector3 targetPos = playerShip.position + new Vector3(fco.targetX, yOffset, 0);
            fco.root.transform.position = Vector3.Lerp(fco.root.transform.position, targetPos, Time.deltaTime * 3f);

            // 투명도 보간
            if (fco.catRenderer != null)
            {
                Color c = fco.catRenderer.color;
                c.a = Mathf.Lerp(c.a, fco.targetAlpha, Time.deltaTime * 3f);
                fco.catRenderer.color = c;
            }

            // 이름 라벨도 투명도 맞추기
            if (fco.nameLabel != null)
            {
                Color nc = fco.nameLabel.color;
                nc.a = Mathf.Lerp(nc.a, fco.targetAlpha * 0.8f, Time.deltaTime * 3f);
                fco.nameLabel.color = nc;
            }
        }
    }

    void CreateFriendCat(FriendData fd)
    {
        GameObject root = new GameObject($"FriendCat_{fd.friendName}");
        root.transform.SetParent(transform);

        // 고양이 스프라이트 — 임시로 원형 마커, 나중에 catType에 따라 교체
        // 지금은 플레이어 고양이와 같은 스프라이트를 복제해서 사용
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = friendSortingOrder;

        // 플레이어 고양이 스프라이트 복사
        if (playerShip != null)
        {
            SpriteRenderer playerSR = playerShip.GetComponent<SpriteRenderer>();
            if (playerSR != null && playerSR.sprite != null)
            {
                sr.sprite = playerSR.sprite;
            }
        }

        // 스프라이트가 없으면 간단한 마커 생성
        if (sr.sprite == null)
        {
            Texture2D marker = new Texture2D(8, 8);
            marker.filterMode = FilterMode.Point;
            Color[] px = new Color[64];
            for (int i = 0; i < 64; i++) px[i] = new Color(1f, 0.8f, 0.3f, 0.7f);
            marker.SetPixels(px);
            marker.Apply();
            sr.sprite = Sprite.Create(marker, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 16);
        }

        // 친구 고양이는 약간 작고 투명하게
        root.transform.localScale = Vector3.one * friendCatScale;
        Color color = sr.color;
        color.a = 0f; // 페이드인 시작
        sr.color = color;

        // 이름 라벨
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(root.transform);
        labelObj.transform.localPosition = new Vector3(0, nameYOffset, 0);

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = fd.friendName;
        tmp.fontSize = 2;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0f); // 페이드인 시작
        tmp.sortingOrder = friendSortingOrder + 1;

        // RectTransform 크기 조절
        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4f, 1f);

        FriendCatObject fco = new FriendCatObject
        {
            root = root,
            catRenderer = sr,
            nameLabel = tmp,
            targetX = 0,
            targetAlpha = friendCatAlpha,
            alive = true
        };

        activeFriendCats[fd.steamId] = fco;
    }

    void HideAll()
    {
        foreach (var kvp in activeFriendCats)
        {
            if (kvp.Value.root != null)
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
