using UnityEngine;

/// <summary>
/// Kontroler Kamera untuk scene Room Creator.
/// Mendukung Right-Click Drag (Pan Kamera) dan Mouse Scroll Wheel (Zoom In/Out).
/// </summary>
public class RoomCreatorCameraController : MonoBehaviour
{
    [Header("Camera Pan Settings")]
    [SerializeField] private Camera cam;
    [SerializeField] private float panSpeed = 1.0f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float minZoom = 3.0f;
    [SerializeField] private float maxZoom = 20.0f;

    private Vector3 dragOrigin;

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        HandleRightClickPan();
        HandleZoom();
    }

    private void HandleRightClickPan()
    {
        // Kapan klik kanan mulai ditekan
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = GetMouseWorldPosition();
        }

        // Selama klik kanan ditahan
        if (Input.GetMouseButton(1))
        {
            Vector3 currentMouseWorldPos = GetMouseWorldPosition();
            Vector3 difference = dragOrigin - currentMouseWorldPos;

            // Geser posisi kamera mengikuti pergerakan klik kanan mouse
            cam.transform.position += new Vector3(difference.x, difference.y, 0f) * panSpeed;
        }
    }

    private void HandleZoom()
    {
        if (!enableZoom || cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed * 5f, minZoom, maxZoom);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        float distZ = Mathf.Abs(cam.transform.position.z);
        if (distZ < 0.001f) distZ = 10f;

        Vector3 mousePoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distZ);
        Vector3 worldPos = cam.ScreenToWorldPoint(mousePoint);
        worldPos.z = 0f;
        return worldPos;
    }
}
