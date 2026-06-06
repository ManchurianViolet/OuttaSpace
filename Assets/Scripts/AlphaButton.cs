using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AlphaButton : MonoBehaviour
{
    void Start()
    {
        Image image = GetComponent<Image>();
        
        // 0.5f 설정 시 알파(투명도) 값이 50% 이상인 부분만 클릭을 감지합니다.
        // 완전히 불투명한 곳만 누르게 하려면 0.1f ~ 0.5f 사이로 조절하시면 됩니다.
        image.alphaHitTestMinimumThreshold = 0.1f;
    }
}