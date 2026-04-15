using UnityEngine;
using System.Collections.Generic;

public class ParallaxStarfield : MonoBehaviour
{
    [Header("Star Settings")]
    public int starCount = 120;
    public int layerCount = 3;
    public float[] layerSpeeds = { 0.3f, 0.8f, 1.5f };
    public float[] layerAlphas = { 0.25f, 0.5f, 0.85f };
    public float[] layerSizes = { 0.04f, 0.06f, 0.1f };

    [Header("Coverage")]
    public float extraPadding = 4f;

    [Header("Boost Stretch")]
    public float boostStretchX = 4f;     // 부스트 시 가로 배율
    public float boostSquishY = 0.3f;    // 부스트 시 세로 압축

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
            float gameSpeed = (float)GameManager.Instance.GetEffectiveSpeed();
            speedMultiplier = Mathf.Min(Mathf.Log10(Mathf.Max(gameSpeed, 1f)) * 0.8f + 0.3f, 20f);
        }
        else if (GameManager.Instance != null && GameManager.Instance.isDocked)
        {
            speedMultiplier = 0.05f;
        }

        bool boosting = BoosterSystem.Instance != null && BoosterSystem.Instance.isBoosting;
        // 부스트 스트레치 부드럽게 전환
        float targetStretchX = boosting ? boostStretchX : 1f;
        float targetSquishY = boosting ? boostSquishY : 1f;

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
            float sz = baseSize * Mathf.Max(zoomScale * 0.7f, 1f);

            // 스트레치 보간 (부드럽게 전환)
            Vector3 currentScale = go.transform.localScale;
            float stretchX = Mathf.Lerp(currentScale.x, sz * targetStretchX, Time.deltaTime * 8f);
            float stretchY = Mathf.Lerp(currentScale.y, sz * targetSquishY, Time.deltaTime * 8f);
            go.transform.localScale = new Vector3(stretchX, stretchY, 1f);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            float twinkle = layerAlphas[star.layer] *
                (0.3f + 0.7f * Mathf.Sin(Time.time * 4f + star.twinkleOffset));

            if (boosting)
                sr.color = new Color(0.6f, 0.75f, 1f, twinkle * 1.3f);
            else
                sr.color = new Color(0.75f, 0.82f, 1f, twinkle);
        }
    }

    void OnDestroy()
    {
        if (pixelTex != null) Destroy(pixelTex);
    }
}