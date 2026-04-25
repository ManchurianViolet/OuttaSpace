using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 고양이 가챠. 정박 화면의 가챠 버튼에서 호출.
/// 슬롯머신 애니메이션 → 결과 표시 → 도감 추가 or 환급.
/// 
/// GachaPanel에 붙이기.
/// </summary>
public class GachaSystem : MonoBehaviour
{
    public static GachaSystem Instance { get; private set; }

    [Header("UI References")]
    public GameObject gachaPanel;           // 가챠 애니메이션 패널
    public Image slotMachineImage;          // 돌아가는 고양이 이미지
    public TextMeshProUGUI resultNameText;  // 결과 고양이 이름
    public TextMeshProUGUI resultStatusText; // "이미 보유" / "신규!" 등
    public Image resultBorder;               // 레어도 테두리
    public Button confirmButton;             // 결과 확인 버튼
    public GameObject resultUI;              // 결과 화면 (슬롯 끝난 후)

    [Header("Animation")]
    public float totalDuration = 5f;        // 전체 애니메이션 길이
    public float startInterval = 0.03f;     // 시작 교체 간격 (빠름)
    public float endInterval = 0.4f;        // 끝 교체 간격 (느림)

    private bool isRolling;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (gachaPanel != null) gachaPanel.SetActive(false);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    /// <summary>
    /// 가챠 시도. 조건 안 되면 false.
    /// </summary>
    public bool TryGacha()
    {
        if (isRolling) return false;
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.credits < CatDatabase.GACHA_COST) return false;

        GameManager.Instance.credits -= CatDatabase.GACHA_COST;
        GameManager.Instance.NotifyStatsChanged();
        StartCoroutine(GachaRoll());
        return true;
    }

    IEnumerator GachaRoll()
    {
        isRolling = true;

        if (gachaPanel != null) gachaPanel.SetActive(true);
        if (resultUI != null) resultUI.SetActive(false);

        int finalId = CatDatabase.Instance.RollGachaId();

        // 슬롯머신: 빠르게 → 느리게
        float elapsed = 0f;
        float nextSwap = 0f;

        while (elapsed < totalDuration)
        {
            if (elapsed >= nextSwap)
            {
                // 랜덤 고양이 표시
                int randomId = Random.Range(0, CatDatabase.TOTAL_COUNT);
                CatData data = CatDatabase.Instance.Get(randomId);
                if (slotMachineImage != null && data != null && data.sprite != null)
                    slotMachineImage.sprite = data.sprite;

                // 간격 점점 늘어남 (이징)
                float t = elapsed / totalDuration;
                float eased = t * t;
                float interval = Mathf.Lerp(startInterval, endInterval, eased);
                nextSwap = elapsed + interval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종 결과
        CatData finalData = CatDatabase.Instance.Get(finalId);
        if (slotMachineImage != null && finalData != null)
            slotMachineImage.sprite = finalData.sprite;

        // 결과 표시
        bool isNew = CatManager.Instance.AddFromGacha(finalId);

        if (resultUI != null) resultUI.SetActive(true);
        if (resultNameText != null)
            resultNameText.text = Loc.Get(finalData.nameKey);
        if (resultStatusText != null)
        {
            if (isNew)
                resultStatusText.text = Loc.Get("gacha_new");
            else
                resultStatusText.text = Loc.Get("gacha_already_owned", CatDatabase.GACHA_REFUND);
        }
        if (resultBorder != null)
            resultBorder.color = CatDatabase.GetRarityColor(finalData.rarity);

        // 중복이면 환급
        if (!isNew)
        {
            GameManager.Instance.credits += CatDatabase.GACHA_REFUND;
            GameManager.Instance.NotifyStatsChanged();
        }

        isRolling = false;
    }

    void OnConfirm()
    {
        if (gachaPanel != null) gachaPanel.SetActive(false);
    }
}
