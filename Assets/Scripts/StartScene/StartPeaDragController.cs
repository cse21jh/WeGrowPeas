using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Picks the visible front pea and pulls its dynamic body with a spring.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BGVisualController))]
public sealed class StartPeaDragController : MonoBehaviour
{
    [SerializeField] private Camera dragCamera;
    [Tooltip("커서를 따라오는 스프링 속도. 높을수록 빠르고 단단하게 따라옵니다.")]
    [SerializeField, Range(1f, 15f)] private float followFrequency = 5f;
    [Tooltip("낮을수록 더 뾰잉거리며, 높을수록 부드럽게 멈춥니다.")]
    [SerializeField, Range(0.1f, 1f)] private float dampingRatio = 0.65f;
    [Tooltip("완두콩을 더미에서 끌어내는 최대 힘(질량당)")]
    [SerializeField, Min(1f)] private float pullForcePerMass = 200f;
    [Tooltip("커서와 잡은 지점이 이 거리만큼 벌어지면 최대로 늘어납니다. 월드 단위입니다.")]
    [SerializeField, Min(0.01f)] private float stretchDistance = 0.3f;
    [Tooltip("놓을 때 유지하는 최대 이동 속도. 너무 멀리 날아가는 것을 줄입니다.")]
    [SerializeField, Min(0f)] private float maxReleaseSpeed = 4f;

    private BGVisualController background;
    private StartPeaAppearanceController draggedPea;
    private Rigidbody2D draggedBody;
    private PeaSquashController squash;
    private TargetJoint2D dragJoint;
    private float originalGravity;
    private RigidbodyInterpolation2D originalInterpolation;
    private CollisionDetectionMode2D originalCollisionDetection;
    private bool isDragging;
    private bool draggingWithMouse;
    private EventSystem uiEventSystem;
    private PointerEventData pointerEvent;
    private readonly List<RaycastResult> uiHits = new();

    public StartPeaAppearanceController DraggedPea => draggedPea;
    public bool IsDragging => isDragging;

    private void Awake() => background = GetComponent<BGVisualController>();

    private void Update()
    {
        if (isDragging && (draggedBody == null || !draggedBody.gameObject.activeInHierarchy ||
            !draggedBody.simulated || background == null || !background.isActiveAndEnabled || Time.timeScale <= 0f))
            EndDrag(true);

        if (Input.GetKeyDown(KeyCode.Escape)) EndDrag(true);
        if (Input.GetMouseButtonDown(0) && !isDragging)
            draggingWithMouse = TryBeginDrag(Input.mousePosition);

        if (!draggingWithMouse) return;
        if (!Input.GetMouseButton(0)) EndDrag();
        else DragTo(Input.mousePosition);
    }

    public bool TryBeginDrag(Vector2 screenPosition)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || isDragging || Time.timeScale <= 0f ||
            background == null || !background.isActiveAndEnabled || IsOverUI(screenPosition)) return false;
        if (dragCamera == null) dragCamera = Camera.main;
        if (dragCamera == null || !dragCamera.pixelRect.Contains(screenPosition)) return false;

        StartPeaAppearanceController picked = null;
        Vector2 pickedPoint = default;
        foreach (StartPeaAppearanceController pea in background.SpawnedPeas)
        {
            if (pea == null || !pea.isActiveAndEnabled ||
                (dragCamera.cullingMask & (1 << pea.gameObject.layer)) == 0 ||
                !TryWorldPoint(screenPosition, pea.transform.position.z, out Vector2 point) ||
                !pea.ContainsVisualPoint(point)) continue;
            if (picked != null && !IsInFront(pea, picked)) continue;
            picked = pea;
            pickedPoint = point;
        }
        if (picked == null) return false;
        Rigidbody2D body = picked.GetComponent<Rigidbody2D>();
        if (body == null || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return false;

        draggedPea = picked;
        draggedBody = body;
        squash = picked.GetComponent<PeaSquashController>();
        originalGravity = body.gravityScale;
        originalInterpolation = body.interpolation;
        originalCollisionDetection = body.collisionDetectionMode;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.WakeUp();

        dragJoint = body.gameObject.AddComponent<TargetJoint2D>();
        dragJoint.autoConfigureTarget = false;
        // Preserve the grabbed point instead of snapping the pea's center to the cursor.
        dragJoint.anchor = body.transform.InverseTransformPoint(pickedPoint);
        dragJoint.target = pickedPoint;
        dragJoint.frequency = Mathf.Max(1f, followFrequency);
        dragJoint.dampingRatio = Mathf.Clamp01(dampingRatio);
        dragJoint.maxForce = body.mass * Mathf.Max(1f, pullForcePerMass);
        isDragging = true;
        background.SetDraggedPea(picked);
        if (squash != null) squash.SetDragPull(Vector2.up, 0f);
        return true;
    }

    public void DragTo(Vector2 screenPosition)
    {
        if (!isDragging) return;
        if (dragJoint == null || draggedBody == null || dragCamera == null)
        {
            EndDrag(true);
            return;
        }
        Rect rect = dragCamera.pixelRect;
        screenPosition.x = Mathf.Clamp(screenPosition.x, rect.xMin, rect.xMax);
        screenPosition.y = Mathf.Clamp(screenPosition.y, rect.yMin, rect.yMax);
        if (!TryWorldPoint(screenPosition, draggedBody.transform.position.z, out Vector2 target)) return;
        dragJoint.target = target;
        Vector2 anchor = draggedBody.transform.TransformPoint(dragJoint.anchor);
        Vector2 pull = target - anchor;
        if (squash != null) squash.SetDragPull(pull, pull.magnitude / Mathf.Max(0.01f, stretchDistance));
    }

    public void EndDrag(bool cancel = false)
    {
        if (dragJoint != null)
        {
            dragJoint.enabled = false;
            Destroy(dragJoint);
        }
        if (draggedBody != null)
        {
            draggedBody.gravityScale = originalGravity;
            draggedBody.interpolation = originalInterpolation;
            draggedBody.collisionDetectionMode = originalCollisionDetection;
            draggedBody.linearVelocity = cancel ? Vector2.zero :
                Vector2.ClampMagnitude(draggedBody.linearVelocity, Mathf.Max(0f, maxReleaseSpeed));
            draggedBody.angularVelocity = cancel ? 0f : Mathf.Clamp(draggedBody.angularVelocity, -360f, 360f);
            draggedBody.WakeUp();
        }
        if (squash != null) squash.ReleaseDrag();
        if (background != null) background.SetDraggedPea(null);
        draggedPea = null;
        draggedBody = null;
        dragJoint = null;
        squash = null;
        isDragging = false;
        draggingWithMouse = false;
    }

    private bool TryWorldPoint(Vector2 screenPosition, float worldZ, out Vector2 point)
    {
        Ray ray = dragCamera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ));
        if (plane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        point = default;
        return false;
    }

    private static bool IsInFront(StartPeaAppearanceController a, StartPeaAppearanceController b)
    {
        SortingGroup first = a.SortingGroup;
        SortingGroup second = b.SortingGroup;
        if (first == null || second == null) return first != null;
        int firstLayer = SortingLayer.GetLayerValueFromID(first.sortingLayerID);
        int secondLayer = SortingLayer.GetLayerValueFromID(second.sortingLayerID);
        if (firstLayer != secondLayer) return firstLayer > secondLayer;
        if (first.sortingOrder != second.sortingOrder) return first.sortingOrder > second.sortingOrder;
        return a.transform.position.z < b.transform.position.z;
    }

    private bool IsOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        if (uiEventSystem != EventSystem.current || pointerEvent == null)
        {
            uiEventSystem = EventSystem.current;
            pointerEvent = new PointerEventData(uiEventSystem);
        }
        pointerEvent.Reset();
        pointerEvent.position = screenPosition;
        uiHits.Clear();
        uiEventSystem.RaycastAll(pointerEvent, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            if (hit.module is GraphicRaycaster) return true;
        }
        return false;
    }

    private void OnDisable() => EndDrag(true);
    private void OnApplicationFocus(bool focused) { if (!focused) EndDrag(true); }
    private void OnApplicationPause(bool paused) { if (paused) EndDrag(true); }
}
