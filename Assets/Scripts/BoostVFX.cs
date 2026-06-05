using UnityEngine;

/// <summary>
/// 부스트 시 시각 효과:
/// 1. 컬러 파티클이 줄 형태로 왼쪽으로 스쳐 지나감 (워프 느낌)
/// 2. 배경색이 살짝 파랗게 변함
/// 
/// 빈 GameObject에 붙이기. 카메라 자식으로 넣으면 좋음.
/// </summary>
public class BoostVFX : MonoBehaviour
{
    [Header("Warp Streaks")]
    public int maxStreaks = 15;
    public float streakSpeed = 25f;
    public float streakLength = 0.8f;      // 줄 길이
    public float streakHeight = 0.015f;    // 줄 두께
    public float spawnInterval = 0.08f;    // 생성 간격

    [Header("Background Shift")]
    public Color boostTint = new Color(0.03f, 0.04f, 0.12f, 1f); // 파란 톤 추가 (명왕성 이전)
    public float tintSpeed = 3f;

    // 명왕성 통과 후엔 부스트마다 랜덤 색 (초/노/빨/파/보/남)
    private Color[] interstellarTints = new Color[]
    {
        new Color(0.03f, 0.12f, 0.04f, 1f),  // 초록
        new Color(0.12f, 0.10f, 0.02f, 1f),  // 노랑
        new Color(0.12f, 0.03f, 0.03f, 1f),  // 빨강
        new Color(0.03f, 0.04f, 0.12f, 1f),  // 파랑
        new Color(0.08f, 0.03f, 0.12f, 1f),  // 보라
        new Color(0.04f, 0.03f, 0.18f, 1f),  // 남색
    };
    private Color currentInterstellarTint;

    private Texture2D streakTex;
    private GameObject[] streaks;
    private SpriteRenderer[] streakRenderers;
    private float[] streakSpeeds;
    private bool[] streakActive;
    private float spawnTimer;
    private int nextStreakIndex;

    private Color normalBgColor;
    private Color targetBgColor;
    private bool wasBoostingLastFrame;

    // 스트릭 색상 팔레트 (하늘색~보라~핑크)
    private Color[] streakColors = new Color[]
    {
        new Color(0.4f, 0.7f, 1f, 0.6f),    // 하늘색
        new Color(0.5f, 0.5f, 1f, 0.5f),    // 연보라
        new Color(0.7f, 0.4f, 1f, 0.4f),    // 보라
        new Color(0.3f, 0.9f, 1f, 0.5f),    // 시안
        new Color(0.6f, 0.3f, 0.9f, 0.3f),  // 진보라
    };

    void Start()
    {
        normalBgColor = Camera.main.backgroundColor;

        // 1x1 흰색 텍스처 (스트릭용)
        streakTex = new Texture2D(1, 1);
        streakTex.SetPixel(0, 0, Color.white);
        streakTex.Apply();
        streakTex.filterMode = FilterMode.Point;

        Sprite streakSprite = Sprite.Create(streakTex, new Rect(0, 0, 1, 1), new Vector2(1f, 0.5f), 1);

        // 스트릭 풀 생성
        streaks = new GameObject[maxStreaks];
        streakRenderers = new SpriteRenderer[maxStreaks];
        streakSpeeds = new float[maxStreaks];
        streakActive = new bool[maxStreaks];

        for (int i = 0; i < maxStreaks; i++)
        {
            GameObject go = new GameObject($"WarpStreak_{i}");
            go.transform.SetParent(transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = streakSprite;
            sr.sortingOrder = -50; // 별 앞, 고양이 뒤
            sr.color = Color.clear;

            go.SetActive(false);

            streaks[i] = go;
            streakRenderers[i] = sr;
            streakActive[i] = false;
        }
    }

    void Update()
    {
        bool boosting = BoosterSystem.Instance != null && BoosterSystem.Instance.isBoosting;
        bool isDocked = GameManager.Instance != null && GameManager.Instance.isDocked;

        if (isDocked)
        {
            HideAllStreaks();
            return;
        }

        // ========== 배경색 전환 ==========
        if (boosting && !wasBoostingLastFrame)
        {
            // 부스트 시작: 현재 배경색 저장
            normalBgColor = Camera.main.backgroundColor;

            // 명왕성 통과 후엔 매 부스트마다 랜덤 색
            if (GameManager.Instance != null && GameManager.Instance.IsInterstellarUnlocked())
            {
                currentInterstellarTint = interstellarTints[Random.Range(0, interstellarTints.Length)];
            }
        }

        if (boosting)
        {
            // interstellar이면 랜덤 색, 아니면 기본 파란 톤
            bool interstellar = GameManager.Instance != null && GameManager.Instance.IsInterstellarUnlocked();
            Color tint = interstellar ? currentInterstellarTint : boostTint;
            targetBgColor = normalBgColor + tint;
            targetBgColor.a = 1f;
        }
        else
        {
            targetBgColor = normalBgColor;
        }

        Camera.main.backgroundColor = Color.Lerp(
            Camera.main.backgroundColor, targetBgColor, tintSpeed * Time.deltaTime);

        wasBoostingLastFrame = boosting;

        // ========== 워프 스트릭 ==========
        if (boosting)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnStreak();
            }
        }

        // 활성 스트릭 이동
        Camera cam = Camera.main;
        float halfW = cam.orthographicSize * cam.aspect + 2f;
        float halfH = cam.orthographicSize + 1f;

        for (int i = 0; i < maxStreaks; i++)
        {
            if (!streakActive[i]) continue;

            streaks[i].transform.position += Vector3.left * streakSpeeds[i] * Time.deltaTime;

            // 화면 왼쪽 밖으로 나가면 비활성화
            if (streaks[i].transform.position.x < cam.transform.position.x - halfW)
            {
                streaks[i].SetActive(false);
                streakActive[i] = false;
            }
            else
            {
                // 서서히 페이드아웃
                Color c = streakRenderers[i].color;
                c.a = Mathf.Max(c.a - Time.deltaTime * 1.5f, 0f);
                streakRenderers[i].color = c;
            }
        }
    }

    void SpawnStreak()
    {
        Camera cam = Camera.main;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        // 다음 슬롯 찾기
        int idx = nextStreakIndex;
        nextStreakIndex = (nextStreakIndex + 1) % maxStreaks;

        // 화면 오른쪽 밖에서 생성, 랜덤 Y
        float x = cam.transform.position.x + halfW + 1f;
        float y = cam.transform.position.y + Random.Range(-halfH * 0.8f, halfH * 0.8f);

        streaks[idx].transform.position = new Vector3(x, y, 0);

        // 랜덤 길이/두께/속도
        float len = streakLength * Random.Range(0.5f, 1.5f);
        float h = streakHeight * Random.Range(0.7f, 1.3f);
        streaks[idx].transform.localScale = new Vector3(len, h, 1f);

        // 랜덤 속도
        streakSpeeds[idx] = streakSpeed * Random.Range(0.7f, 1.3f);

        // 랜덤 색상
        Color c = streakColors[Random.Range(0, streakColors.Length)];
        c.a = Random.Range(0.3f, 0.7f);
        streakRenderers[idx].color = c;

        streaks[idx].SetActive(true);
        streakActive[idx] = true;
    }

    void HideAllStreaks()
    {
        for (int i = 0; i < maxStreaks; i++)
        {
            if (streakActive[i])
            {
                streaks[i].SetActive(false);
                streakActive[i] = false;
            }
        }
    }

    void OnDestroy()
    {
        if (streakTex != null) Destroy(streakTex);
    }
}
