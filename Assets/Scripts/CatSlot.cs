using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 그리드의 한 칸.
/// Image (고양이) + Image (무지개 테두리) + Button.
/// Panel에 붙이기.
/// </summary>
public class CatSlot : MonoBehaviour
{
    [Header("References")]
    public Image catImage;
    public Image rarityBorder;      // 레어도 색상 테두리 (옵션)
    public Image rainbowBorder;     // 무지개 테두리 (현재 사용 중)
    public Button button;

    private int catId = -1;
    private bool isOwned;

    void Start()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void Setup(int id, Sprite sprite, bool owned, CatRarity rarity)
    {
        catId = id;
        isOwned = owned;

        if (catImage != null)
        {
            catImage.sprite = sprite;
            catImage.color = owned ? Color.white : new Color(1, 1, 1, 0.9f);
            catImage.enabled = (sprite != null);
        }

        if (rarityBorder != null)
        {
            rarityBorder.color = CatDatabase.GetRarityColor(rarity);
            rarityBorder.enabled = true;
        }

        if (button != null)
            button.interactable = owned;
    }

    public void Clear()
    {
        catId = -1;
        if (catImage != null) catImage.enabled = false;
        if (rarityBorder != null) rarityBorder.enabled = false;
        if (rainbowBorder != null) rainbowBorder.enabled = false;
        if (button != null) button.interactable = false;
    }

    public void SetRainbowActive(bool active, Color rainbowColor)
    {
        if (rainbowBorder == null) return;
        rainbowBorder.enabled = active;
        if (active) rainbowBorder.color = rainbowColor;
    }

    void OnClick()
    {
        if (!isOwned || catId < 0) return;
        if (CatManager.Instance == null) return;

        CatManager.Instance.SetCurrentCat(catId);

        // 현재 도감 UI 찾아서 닫기 (교체 후 자동 닫힘)
        CatCollectionUI parent = GetComponentInParent<CatCollectionUI>();
        if (parent != null && parent.closeButton != null)
            parent.closeButton.onClick.Invoke();
    }
}
