using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
  public Transform target;
  public float height = 20f;

  [Header("Zoom")]
  public float zoomSpeed = 10f;
  public float minSize = 8f;
  public float maxSize = 30f;

  private Camera miniMapCamera;

  void Awake()
  {
    miniMapCamera = GetComponent<Camera>();
  }

  void LateUpdate()
  {
    if (target == null) return;

    Vector3 newPosition = target.position;
    newPosition.y = target.position.y + height;
    transform.position = newPosition;

    transform.rotation = Quaternion.Euler(90f, 180f, 0f);

    HandleZoom();
  }

  void HandleZoom()
  {
    if (miniMapCamera == null) return;

    if (!IsMouseOverMiniMap()) return;

    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (Mathf.Abs(scroll) < 0.0001f) return;

    miniMapCamera.orthographicSize -= scroll * zoomSpeed;
    miniMapCamera.orthographicSize = Mathf.Clamp(
        miniMapCamera.orthographicSize,
        minSize,
        maxSize
    );
  }

  bool IsMouseOverMiniMap()
  {
    Rect rect = miniMapCamera.rect;

    float mouseX = Input.mousePosition.x / Screen.width;
    float mouseY = Input.mousePosition.y / Screen.height;

    return rect.Contains(new Vector2(mouseX, mouseY));
  }
}
