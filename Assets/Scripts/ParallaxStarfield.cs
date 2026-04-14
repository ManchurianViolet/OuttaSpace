using UnityEngine;
using System.Collections.Generic;

public class ParallaxStarfield : MonoBehaviour
{
    [Header("Star Settings")]
    public int starCount = 120;
    public int layerCount = 3;
    public float[] layerSpeeds = { 0.3f, 0.8f, 1.5f };
    public float[] layerAlphas = { 0.25f, 0.5f, 0.85f };
    public float[] layerSizes = { 0.02f, 0.03f, 0.05f };

    [Header("Coverage")]
    public float extraPadding = 4f;

    private List<Star> stars = new List<Star>();
    private List<GameObject> starObjects = new List<GameObject>();
    private Texture2D pixelTex;
    private Camera cam;

    struct Star
    {
        public int layer;
        public float twinkleOffset;
    }

    void Start()
    {
        cam = Camera.main;
        pixelTex = new Texture2D(1, 1);
        pixelTex.SetPixel(0, 0, Color.white);
        pixelTex.Apply();
        pixelTex.filterMode = FilterMode.Point;
        Sprite pixelSprite = Sprite.Create(pixelTex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);

        float maxWidth = 40f;
        float maxHeight = 30f;

        for (int i = 0; i < starCount; i++)
        {
            int layer = Random.Range(0, layerCount);
            float x = Random.Range(-maxWidth / 2, maxWidth / 2);
            float y = Random.Range(-maxHeight / 2, maxHeight / 2);

            GameObject go = new GameObject($"Star_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(x, y, layer);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.sortingOrder = -100 + layer;

            float size = layerSizes[layer];
            if (Random.value < 0.1f) size *= 1.5f;
            go.transform.localScale = Vector3.one * size;

            Star star = new Star { layer = layer, twinkleOffset = Random.value * Mathf.PI * 2 };
            stars.Add(star);
            starObjects.Add(go);
        }
    }

    void Update()
    {
        if (cam == null) return;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        float halfW = camWidth / 2f + extraPadding;
        float halfH = camHeight / 2f + extraPadding;

        float speedMultiplier = 1f;
        if (GameManager.Instance != null && !GameManager.Instance.isDocked)
        {
            // 부스트 포함된 실제 속도 사용
            float gameSpeed = (float)GameManager.Instance.GetEffectiveSpeed();
            speedMultiplier = Mathf.Min(Mathf.Log10(Mathf.Max(gameSpeed, 1f)) * 1.3f + 0.3f, 10f);
        }
        else if (GameManager.Instance != null && GameManager.Instance.isDocked)
        {
            speedMultiplier = 0.05f;
        }

        // 부스트 중이면 별 색상 살짝 파랗게
        bool boosting = BoosterSystem.Instance != null && BoosterSystem.Instance.isBoosting;

        for (int i = 0; i < starObjects.Count; i++)
        {
            GameObject go = starObjects[i];
            Star star = stars[i];

            float moveSpeed = layerSpeeds[star.layer] * speedMultiplier * Time.deltaTime;
            go.transform.localPosition += Vector3.left * moveSpeed;

            Vector3 pos = go.transform.localPosition;
            if (pos.x < -halfW)
            {
                pos.x = halfW;
                pos.y = Random.Range(-halfH, halfH);
                go.transform.localPosition = pos;
            }
            if (pos.y < -halfH || pos.y > halfH)
            {
                pos.y = Random.Range(-halfH, halfH);
                go.transform.localPosition = pos;
            }

            float zoomScale = cam.orthographicSize / 4f;
            float baseSize = layerSizes[star.layer];
            go.transform.localScale = Vector3.one * baseSize * Mathf.Max(zoomScale * 0.7f, 1f);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            float twinkle = layerAlphas[star.layer] *
                (0.3f + 0.7f * Mathf.Sin(Time.time * 4f + star.twinkleOffset));

            if (boosting)
            {
                // 부스트: 별이 파랗고 밝게
                sr.color = new Color(0.6f, 0.75f, 1f, twinkle * 1.3f);
            }
            else
            {
                sr.color = new Color(0.75f, 0.82f, 1f, twinkle);
            }
        }
    }

    void OnDestroy()
    {
        if (pixelTex != null) Destroy(pixelTex);
    }
}
