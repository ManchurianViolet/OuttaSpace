using UnityEngine;

/// <summary>
/// 코드로 픽셀아트 우주선을 생성하는 스크립트.
/// 빈 GameObject에 붙이면 자동으로 SpriteRenderer + 스프라이트를 생성함.
/// 
/// [사용법]
/// 1. 빈 GameObject 생성 → "Ship" 이름
/// 2. 이 스크립트 붙이기
/// 3. ShipController.cs도 같이 붙이기
/// 4. Play!
/// </summary>
public class PixelShipGenerator : MonoBehaviour
{
    [Header("Ship Type")]
    public ShipDesign design = ShipDesign.Explorer;

    [Header("Rendering")]
    public int pixelsPerUnit = 16;
    public int sortingOrder = 10;

    public enum ShipDesign
    {
        Explorer,    // 기본 탐사선
        Fighter,     // 전투기형
        Cruiser,     // 순양함
        Shuttle      // 셔틀
    }

    private Texture2D shipTexture;

    void Start()
    {
        GenerateShip();
    }

    public void GenerateShip()
    {
        int[,] blueprint = GetBlueprint(design);
        Color[] palette = GetPalette(design);

        int height = blueprint.GetLength(0);
        int halfWidth = blueprint.GetLength(1);
        int width = halfWidth * 2 - 1; // 좌우 대칭

        shipTexture = new Texture2D(width, height);
        shipTexture.filterMode = FilterMode.Point; // 픽셀아트 필수!
        shipTexture.wrapMode = TextureWrapMode.Clamp;

        // 투명으로 초기화
        Color[] clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        shipTexture.SetPixels(clear);

        // 블루프린트 → 텍스처 (좌우 대칭)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < halfWidth; x++)
            {
                int colorIdx = blueprint[y, x];
                if (colorIdx == 0) continue;

                Color c = palette[colorIdx];

                // 블루프린트는 위에서 아래로 정의되어 있으므로 y축 뒤집기
                int texY = height - 1 - y;

                // 오른쪽 절반
                int rightX = halfWidth - 1 + x;
                shipTexture.SetPixel(rightX, texY, c);

                // 왼쪽 절반 (대칭)
                int leftX = halfWidth - 1 - x;
                shipTexture.SetPixel(leftX, texY, c);
            }
        }

        shipTexture.Apply();

        // SpriteRenderer 설정
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = Sprite.Create(
            shipTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), // 중심 피벗
            pixelsPerUnit
        );
        sr.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 우주선 블루프린트. 오른쪽 절반만 정의 (자동으로 좌우 대칭).
    /// 0 = 투명, 1~9 = 팔레트 색상 인덱스.
    /// 위에서 아래 방향 (코뿔 → 엔진).
    /// </summary>
    int[,] GetBlueprint(ShipDesign type)
    {
        switch (type)
        {
            case ShipDesign.Explorer:
                return new int[,]
                {
                    //  중심 →  오른쪽
                    //  x: 0  1  2  3  4  5  6  7
                    {  3, 0, 0, 0, 0, 0, 0, 0 },  // row 0: 코 끝
                    {  3, 3, 0, 0, 0, 0, 0, 0 },  // row 1
                    {  2, 3, 1, 0, 0, 0, 0, 0 },  // row 2
                    {  4, 2, 3, 1, 0, 0, 0, 0 },  // row 3: 조종석
                    {  4, 2, 2, 1, 0, 0, 0, 0 },  // row 4
                    {  2, 2, 2, 2, 1, 0, 0, 0 },  // row 5: 동체
                    {  2, 2, 2, 2, 1, 0, 0, 0 },  // row 6
                    {  2, 2, 2, 2, 2, 1, 0, 0 },  // row 7
                    {  3, 2, 2, 2, 2, 2, 1, 0 },  // row 8: 날개 시작
                    {  3, 2, 2, 2, 2, 2, 2, 1 },  // row 9: 날개 최대
                    {  3, 2, 2, 2, 2, 2, 2, 1 },  // row 10
                    {  1, 2, 2, 2, 2, 2, 1, 0 },  // row 11: 날개 끝
                    {  1, 1, 2, 2, 2, 1, 0, 0 },  // row 12
                    {  0, 1, 1, 2, 1, 1, 0, 0 },  // row 13: 엔진부
                    {  0, 0, 1, 5, 1, 0, 0, 0 },  // row 14: 노즐
                    {  0, 0, 5, 6, 5, 0, 0, 0 },  // row 15: 화염
                    {  0, 0, 0, 6, 0, 0, 0, 0 },  // row 16: 화염 끝
                };

            case ShipDesign.Fighter:
                return new int[,]
                {
                    {  3, 0, 0, 0, 0, 0, 0, 0 },
                    {  4, 3, 0, 0, 0, 0, 0, 0 },
                    {  4, 2, 1, 0, 0, 0, 0, 0 },
                    {  2, 2, 1, 0, 0, 0, 0, 0 },
                    {  2, 2, 2, 1, 0, 0, 0, 0 },
                    {  2, 2, 2, 1, 1, 0, 0, 0 },
                    {  3, 2, 2, 2, 1, 1, 0, 0 },
                    {  3, 2, 2, 2, 2, 2, 1, 1 },  // 날개 뾰족
                    {  3, 2, 2, 2, 2, 1, 1, 0 },
                    {  1, 2, 2, 2, 1, 0, 0, 0 },
                    {  1, 1, 2, 1, 1, 0, 0, 0 },
                    {  0, 1, 5, 1, 0, 0, 0, 0 },
                    {  0, 5, 6, 5, 0, 0, 0, 0 },
                    {  0, 0, 6, 0, 0, 0, 0, 0 },
                };

            case ShipDesign.Cruiser:
                return new int[,]
                {
                    {  3, 0, 0, 0, 0, 0, 0, 0, 0 },
                    {  3, 1, 0, 0, 0, 0, 0, 0, 0 },
                    {  2, 3, 1, 0, 0, 0, 0, 0, 0 },
                    {  4, 2, 3, 1, 0, 0, 0, 0, 0 },
                    {  4, 2, 2, 1, 0, 0, 0, 0, 0 },
                    {  2, 2, 2, 2, 1, 0, 0, 0, 0 },
                    {  2, 2, 2, 2, 2, 1, 0, 0, 0 },
                    {  2, 2, 2, 2, 2, 2, 1, 0, 0 },
                    {  3, 2, 2, 2, 2, 2, 2, 1, 0 },
                    {  3, 2, 2, 2, 2, 2, 2, 2, 1 },  // 넓은 동체
                    {  3, 2, 2, 2, 2, 2, 2, 2, 1 },
                    {  3, 2, 2, 2, 2, 2, 2, 1, 0 },
                    {  2, 2, 2, 2, 2, 2, 1, 0, 0 },
                    {  1, 2, 2, 2, 2, 1, 0, 0, 0 },
                    {  1, 1, 2, 2, 1, 1, 0, 0, 0 },
                    {  0, 1, 5, 5, 1, 0, 0, 0, 0 },  // 쌍 노즐
                    {  0, 5, 6, 6, 5, 0, 0, 0, 0 },
                    {  0, 0, 6, 6, 0, 0, 0, 0, 0 },
                };

            case ShipDesign.Shuttle:
                return new int[,]
                {
                    {  3, 0, 0, 0, 0, 0 },
                    {  2, 3, 0, 0, 0, 0 },
                    {  4, 2, 1, 0, 0, 0 },
                    {  2, 2, 1, 0, 0, 0 },
                    {  2, 2, 2, 1, 0, 0 },
                    {  2, 2, 2, 1, 0, 0 },
                    {  3, 2, 2, 2, 1, 0 },
                    {  3, 2, 2, 2, 1, 1 },
                    {  1, 2, 2, 2, 1, 0 },
                    {  1, 1, 2, 1, 0, 0 },
                    {  0, 1, 5, 1, 0, 0 },
                    {  0, 5, 6, 0, 0, 0 },
                };

            default:
                return GetBlueprint(ShipDesign.Explorer);
        }
    }

    /// <summary>
    /// 색상 팔레트. 인덱스 0 = 투명.
    /// </summary>
    Color[] GetPalette(ShipDesign type)
    {
        switch (type)
        {
            case ShipDesign.Explorer:
                return new Color[]
                {
                    Color.clear,                        // 0: 투명
                    HexColor("#2a3a5e"),                 // 1: 외곽 (짙은 남색)
                    HexColor("#5a7eb8"),                 // 2: 동체 (밝은 파랑)
                    HexColor("#8ab4e8"),                 // 3: 하이라이트 (하늘색)
                    HexColor("#aaddff"),                 // 4: 조종석 (밝은 시안)
                    HexColor("#ff6633"),                 // 5: 노즐 (주황)
                    HexColor("#ffaa33"),                 // 6: 화염 (밝은 주황)
                };

            case ShipDesign.Fighter:
                return new Color[]
                {
                    Color.clear,
                    HexColor("#3a2a2a"),                 // 1: 외곽 (짙은 적갈색)
                    HexColor("#8a4444"),                 // 2: 동체 (적색)
                    HexColor("#cc6666"),                 // 3: 하이라이트
                    HexColor("#ffcc44"),                 // 4: 조종석 (노랑)
                    HexColor("#ff4422"),                 // 5: 노즐
                    HexColor("#ffaa22"),                 // 6: 화염
                };

            case ShipDesign.Cruiser:
                return new Color[]
                {
                    Color.clear,
                    HexColor("#2e2e3e"),                 // 1: 외곽 (회색)
                    HexColor("#6a6a8a"),                 // 2: 동체 (은색)
                    HexColor("#9a9aba"),                 // 3: 하이라이트
                    HexColor("#44ddff"),                 // 4: 조종석 (시안)
                    HexColor("#ff5533"),                 // 5: 노즐
                    HexColor("#ffbb44"),                 // 6: 화염
                };

            case ShipDesign.Shuttle:
                return new Color[]
                {
                    Color.clear,
                    HexColor("#2a3a2a"),                 // 1: 외곽 (짙은 녹색)
                    HexColor("#4a8a5a"),                 // 2: 동체 (녹색)
                    HexColor("#7abb8a"),                 // 3: 하이라이트
                    HexColor("#eeff88"),                 // 4: 조종석 (연두)
                    HexColor("#ff6633"),                 // 5: 노즐
                    HexColor("#ffcc33"),                 // 6: 화염
                };

            default:
                return GetPalette(ShipDesign.Explorer);
        }
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    void OnDestroy()
    {
        if (shipTexture != null) Destroy(shipTexture);
    }

    // ============ 에디터에서 미리보기용 ============
#if UNITY_EDITOR
    [ContextMenu("Regenerate Ship")]
    void RegenerateInEditor()
    {
        GenerateShip();
    }
#endif
}
