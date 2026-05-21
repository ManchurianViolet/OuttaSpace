using UnityEngine;

/// <summary>
/// 마우스 휠로 카메라 줌 인/아웃.
/// 줌 아웃하면 시야가 넓어지면서 목적지 행성/항성이 멀리 보임.
/// Main Camera에 붙이기.
/// </summary>
public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float minZoom = 2f;
    public float maxZoom = 12f;
    public float defaultZoom = 4f;
    public float zoomSpeed = 1.5f;
    public float smoothSpeed = 8f;

    [Header("Zoom Target")]
    public Transform zoomTarget;

    private Camera cam;
    private float targetZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        targetZoom = defaultZoom;
        cam.orthographicSize = defaultZoom;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isDocked)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
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

    public float GetZoomNormalized()
    {
        return Mathf.InverseLerp(minZoom, maxZoom, cam.orthographicSize);
    }

    /// <summary>
    /// 기본 줌으로 즉시 리셋. targetZoom + 실제 orthographicSize 둘 다 즉시 적용.
    /// 정박 중에는 Update에서 Lerp가 작동 안 하므로 직접 강제해야 함.
    /// 카메라 위치도 원점으로 복원 (줌 인 상태에서 ship 따라간 위치 보존되어 있을 수 있음).
    /// </summary>
    public void ResetZoom()
    {
        targetZoom = defaultZoom;
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = defaultZoom;
            Vector3 pos = cam.transform.position;
            pos.x = 0;
            pos.y = 0;
            cam.transform.position = pos;
        }
    }
}
