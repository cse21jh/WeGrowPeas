using UnityEngine;
using UnityEngine.EventSystems;

public class VcamManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform sharedFollowTarget;
    [SerializeField] private BoxCollider2D moveBounds;
    [SerializeField] private VcamController[] vcamControllers;

    // 현재는 무시
    public Camera holdCanvasCamera;

    [Header("Drag")]
    [SerializeField] private bool enableDrag = true;

    [Tooltip("0 = 좌클릭, 1 = 우클릭, 2 = 휠 클릭")]
    [SerializeField] private int dragMouseButton = 0;

    [Tooltip("true면 유니티 에디터 Scene View처럼 화면을 잡아끄는 느낌")]
    [SerializeField] private bool dragLikeSceneView = true;

    [SerializeField] private float dragSensitivity = 1f;

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = true;

    [Tooltip("최대로 확대했을 때의 Orthographic Size")]
    [SerializeField] private float minOrthographicSize = 3f;

    [Tooltip("최대로 축소했을 때의 Orthographic Size")]
    [SerializeField] private float maxOrthographicSize = 12f;

    [SerializeField] private float zoomSensitivity = 0.5f;

    [Tooltip("한 프레임에 처리할 최대 스크롤 양. 빠른 휠 입력으로 Bounds가 튀는 것을 방지")]
    [SerializeField] private float maxScrollPerFrame = 3f;

    [Tooltip("true면 마우스 커서 위치를 기준으로 확대/축소")]
    [SerializeField] private bool zoomAroundMousePosition = true;

    [Header("Smooth Zoom")]
    [SerializeField] private bool enableSmoothZoom = true;

    [Tooltip("작을수록 빠르게 따라감. 0.05~0.15 권장")]
    [SerializeField] private float zoomSmoothTime = 0.08f;

    [Tooltip("목표 줌값과 충분히 가까워지면 바로 스냅해서 미세 떨림 방지")]
    [SerializeField] private float zoomSnapThreshold = 0.001f;

    [Header("Bounds")]
    [Tooltip("targetOrthographicSize를 아직 얻지 못했을 때 사용할 임시 Orthographic Size")]
    [SerializeField] private float fallbackOrthographicSize = 5f;

    [Header("UI")]
    [SerializeField] private bool ignoreInputOnUI = false;

    private bool isDragging;
    private Vector3 previousMousePosition;

    private Vector3 targetPosition;

    // 휠 입력으로 정해지는 목표 줌값
    private float targetOrthographicSize;

    // 실제 vcam에 적용되는 현재 줌값
    private float currentOrthographicSize;

    private float zoomVelocity;

    private void Awake()
    {
        InitializeReferences();

        if (!enabled)
        {
            return;
        }

        InitializeVcams();

        targetPosition = sharedFollowTarget.position;

        targetOrthographicSize = GetCurrentOrthographicSize();
        currentOrthographicSize = targetOrthographicSize;

        ApplyCameraState(true);
    }

    private void Update()
    {
        if (enableDrag)
        {
            HandleDrag();
        }

        if (enableZoom)
        {
            HandleZoom();
        }

        if (enableSmoothZoom)
        {
            ApplyCameraState(false);
        }
    }

    private void InitializeReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[VcamManager] Main Camera가 필요합니다.");
            enabled = false;
            return;
        }

        if (sharedFollowTarget == null)
        {
            Debug.LogError("[VcamManager] Shared Follow Target이 필요합니다.");
            enabled = false;
            return;
        }

        if (moveBounds == null)
        {
            Debug.LogError("[VcamManager] Move Bounds(BoxCollider2D)가 필요합니다.");
            enabled = false;
            return;
        }

        if (vcamControllers == null || vcamControllers.Length == 0)
        {
            vcamControllers = FindObjectsByType<VcamController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        }

        if (vcamControllers == null || vcamControllers.Length == 0)
        {
            Debug.LogError("[VcamManager] VcamController가 하나도 없습니다.");
            enabled = false;
        }
    }

    private void InitializeVcams()
    {
        foreach (VcamController controller in vcamControllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.SetFollowTarget(sharedFollowTarget);
        }
    }

    private void HandleDrag()
    {
        if (mainCamera == null || sharedFollowTarget == null || moveBounds == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            if (IsPointerOverUI())
            {
                return;
            }

            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(dragMouseButton))
        {
            isDragging = false;
        }

        if (!isDragging || !Input.GetMouseButton(dragMouseButton))
        {
            return;
        }

        Vector3 currentMousePosition = Input.mousePosition;

        Vector3 worldDelta = GetMouseDragWorldDelta(
            previousMousePosition,
            currentMousePosition
        );

        targetPosition += worldDelta;

        ApplyCameraState(!enableSmoothZoom);

        previousMousePosition = currentMousePosition;
    }

    private void HandleZoom()
    {
        if (mainCamera == null || sharedFollowTarget == null || moveBounds == null)
        {
            return;
        }

        float scroll = GetScrollInput();

        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        if (IsPointerOverUI())
        {
            return;
        }

        scroll = Mathf.Clamp(scroll, -maxScrollPerFrame, maxScrollPerFrame);

        Vector3 mousePosition = Input.mousePosition;

        Vector3 mouseWorldBeforeZoom = zoomAroundMousePosition
            ? ScreenToWorldByOrthographicSize(mousePosition, targetPosition, targetOrthographicSize)
            : Vector3.zero;

        targetOrthographicSize -= scroll * zoomSensitivity;
        targetOrthographicSize = ClampOrthographicSize(targetOrthographicSize);

        if (zoomAroundMousePosition)
        {
            Vector3 mouseWorldAfterZoom =
                ScreenToWorldByOrthographicSize(mousePosition, targetPosition, targetOrthographicSize);

            Vector3 correction = mouseWorldBeforeZoom - mouseWorldAfterZoom;
            correction.z = 0f;

            targetPosition += correction;
        }

        ApplyCameraState(!enableSmoothZoom);
    }

    private Vector3 GetMouseDragWorldDelta(Vector3 previousScreenPosition, Vector3 currentScreenPosition)
    {
        Vector3 screenDelta = currentScreenPosition - previousScreenPosition;

        float orthographicSize = currentOrthographicSize > 0f
            ? currentOrthographicSize
            : fallbackOrthographicSize;

        float worldHeight = orthographicSize * 2f;
        float worldWidth = worldHeight * mainCamera.aspect;

        float worldPerPixelX = worldWidth / Screen.width;
        float worldPerPixelY = worldHeight / Screen.height;

        Vector3 worldDelta = new Vector3(
            screenDelta.x * worldPerPixelX,
            screenDelta.y * worldPerPixelY,
            0f
        );

        if (dragLikeSceneView)
        {
            worldDelta = -worldDelta;
        }

        worldDelta *= dragSensitivity;

        return worldDelta;
    }

    private float GetScrollInput()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Approximately(scroll, 0f))
        {
            scroll = Input.GetAxisRaw("Mouse ScrollWheel") * 10f;
        }

        return scroll;
    }

    private Vector3 ScreenToWorldByOrthographicSize(
        Vector3 screenPosition,
        Vector3 cameraCenter,
        float orthographicSize
    )
    {
        Vector3 viewportPoint = mainCamera.ScreenToViewportPoint(screenPosition);

        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * mainCamera.aspect;

        float worldX = cameraCenter.x + (viewportPoint.x - 0.5f) * halfWidth * 2f;
        float worldY = cameraCenter.y + (viewportPoint.y - 0.5f) * halfHeight * 2f;

        return new Vector3(worldX, worldY, cameraCenter.z);
    }

    private void ApplyCameraState(bool instant)
    {
        targetOrthographicSize = ClampOrthographicSize(targetOrthographicSize);

        if (instant || !enableSmoothZoom)
        {
            currentOrthographicSize = targetOrthographicSize;
            zoomVelocity = 0f;
        }
        else
        {
            currentOrthographicSize = Mathf.SmoothDamp(
                currentOrthographicSize,
                targetOrthographicSize,
                ref zoomVelocity,
                zoomSmoothTime
            );

            if (Mathf.Abs(currentOrthographicSize - targetOrthographicSize) <= zoomSnapThreshold)
            {
                currentOrthographicSize = targetOrthographicSize;
                zoomVelocity = 0f;
            }
        }

        currentOrthographicSize = ClampOrthographicSize(currentOrthographicSize);

        // 위치는 목표 줌값 기준으로 먼저 제한.
        // 빠른 줌아웃 시 최종 화면 크기가 Bounds 밖으로 나가는 것을 방지한다.
        targetPosition = ClampPositionToBounds(targetPosition, targetOrthographicSize);

        ApplyOrthographicSizeToAllVcams(currentOrthographicSize);
        ApplyTargetPosition(targetPosition);
    }

    private Vector3 ClampPositionToBounds(Vector3 position)
    {
        return ClampPositionToBounds(position, targetOrthographicSize);
    }

    private Vector3 ClampPositionToBounds(Vector3 position, float orthographicSize)
    {
        Bounds bounds = moveBounds.bounds;

        float halfHeight = orthographicSize;
        float halfWidth = orthographicSize * mainCamera.aspect;

        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        Vector3 clamped = position;

        if (minX > maxX)
        {
            clamped.x = bounds.center.x;
        }
        else
        {
            clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        }

        if (minY > maxY)
        {
            clamped.y = bounds.center.y;
        }
        else
        {
            clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
        }

        return clamped;
    }

    private float ClampOrthographicSize(float size)
    {
        if (mainCamera == null || moveBounds == null)
        {
            return Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
        }

        Bounds bounds = moveBounds.bounds;

        float maxSizeByHeight = bounds.size.y * 0.5f;
        float maxSizeByWidth = bounds.size.x / (2f * mainCamera.aspect);

        float allowedMaxSize = Mathf.Min(
            maxOrthographicSize,
            maxSizeByHeight,
            maxSizeByWidth
        );

        allowedMaxSize = Mathf.Max(0.01f, allowedMaxSize);

        float allowedMinSize = Mathf.Min(minOrthographicSize, allowedMaxSize);

        return Mathf.Clamp(size, allowedMinSize, allowedMaxSize);
    }

    private float GetCurrentOrthographicSize()
    {
        if (vcamControllers != null)
        {
            foreach (VcamController controller in vcamControllers)
            {
                if (controller == null)
                {
                    continue;
                }

                float size = controller.GetOrthographicSize();

                if (size > 0f)
                {
                    return size;
                }
            }
        }

        if (mainCamera != null)
        {
            return mainCamera.orthographicSize;
        }

        return fallbackOrthographicSize;
    }

    private void ApplyTargetPosition(Vector3 position)
    {
        sharedFollowTarget.position = new Vector3(
            position.x,
            position.y,
            sharedFollowTarget.position.z
        );
    }

    private void ApplyOrthographicSizeToAllVcams(float size)
    {
        if (vcamControllers == null)
        {
            return;
        }

        foreach (VcamController controller in vcamControllers)
        {
            if (controller == null)
            {
                continue;
            }

            controller.SetOrthographicSize(size);
        }
    }

    private bool IsPointerOverUI()
    {
        if (!ignoreInputOnUI)
        {
            return false;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDrawGizmosSelected()
    {
        if (moveBounds == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        Bounds bounds = moveBounds.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
