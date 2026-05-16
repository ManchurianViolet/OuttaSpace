using UnityEngine;

/// <summary>
/// 마우스 휠로 카메라 줌 인/아웃.
/// 줌 아웃하면 시야가 넓어지면서 목적지 행성/항성이 멀리 보임.
/// Main Camera에 붙이기.
/// </summary>
public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 2f;      // 최대 확대 (카메라 size 작을수록 확대)
    public float maxZoom = 12f;     // 최대 축소
    public float defaultZoom = 4f;
    public float zoomSpeed = 1.5f;
    public float smoothSpeed = 8f;  // 부드러운 전환 속도

    [Header("Zoom Target")]
    public Transform zoomTarget; // Inspector에서 Ship 오브젝트 드래그

    private Camera cam;
    private float targetZoom;

    void Start()
    {
        EnsureCamera();
        targetZoom = defaultZoom;
        cam.orthographicSize = defaultZoom;
    }

    void EnsureCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isDocked)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float prevSize = cam.orthographicSize;
            targetZoom -= scroll * zoomSpeed * targetZoom * 0.5f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        float oldSize = cam.orthographicSize;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothSpeed);

        // 고양이 기준으로 줌
        if (zoomTarget != null && oldSize != cam.orthographicSize)
        {
            float ratio = cam.orthographicSize / oldSize;
            Vector3 catPos = zoomTarget.position;
            Vector3 camPos = cam.transform.position;
            camPos.x = catPos.x + (camPos.x - catPos.x) * ratio;
            camPos.y = catPos.y + (camPos.y - catPos.y) * ratio;
            cam.transform.position = camPos;
        }
    }

    /// <summary>
    /// 현재 줌 레벨 (0=최대확대, 1=최대축소)
    /// </summary>
    public float GetZoomNormalized()
    {
        return Mathf.InverseLerp(minZoom, maxZoom, cam != null ? cam.orthographicSize : defaultZoom);
    }

    /// <summary>
    /// 기본 줌으로 즉시 리셋.
    /// docked 상태에서는 Update가 일찍 return하므로 Lerp가 일어나지 않아
    /// orthographicSize도 같이 강제로 설정해야 한다.
    /// </summary>
    public void ResetZoom()
    {
        EnsureCamera();
        targetZoom = defaultZoom;
        if (cam != null) cam.orthographicSize = defaultZoom;
    }
}