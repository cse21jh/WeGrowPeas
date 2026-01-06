using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragMoveRect : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Target to move (usually PhoneRoot)")]
    [SerializeField] private RectTransform target;

    [Header("Constraints (optional)")]
    [SerializeField] private RectTransform clampWithin; // 보통 Canvas의 RectTransform (없으면 클램프 안 함)
    [SerializeField] private bool clamp = true;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Vector2 _pointerOffsetInTarget; // target pivot 기준 포인터 오프셋

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[UIDragMoveRect] Canvas not found in parents.");
            enabled = false;
            return;
        }

        _canvasRect = _canvas.transform as RectTransform;
        if (clampWithin == null) clampWithin = _canvasRect;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null) return;

        // 포인터 위치를 Canvas 로컬로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out var canvasLocalPoint
        );

        // target의 현재 anchoredPosition(=pivot 기준)에서 포인터까지 오프셋 저장
        _pointerOffsetInTarget = target.anchoredPosition - canvasLocalPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out var canvasLocalPoint
        );

        var newPos = canvasLocalPoint + _pointerOffsetInTarget;

        if (clamp && clampWithin != null)
            newPos = ClampAnchoredPosition(target, clampWithin, newPos);

        target.anchoredPosition = newPos;
    }

    private static Vector2 ClampAnchoredPosition(RectTransform target, RectTransform bounds, Vector2 desiredAnchoredPos)
    {
        // target/bounds는 같은 Canvas 공간(로컬) 기준이라고 가정
        // pivot 기반 anchoredPosition을 bounds 안에 들어오도록 제한

        // bounds의 로컬 rect
        var b = bounds.rect;

        // target의 크기
        var size = target.rect.size;

        // pivot에 따라 target의 extents 계산
        var left = size.x * target.pivot.x;
        var right = size.x * (1f - target.pivot.x);
        var bottom = size.y * target.pivot.y;
        var top = size.y * (1f - target.pivot.y);

        float minX = b.xMin + left;
        float maxX = b.xMax - right;
        float minY = b.yMin + bottom;
        float maxY = b.yMax - top;

        desiredAnchoredPos.x = Mathf.Clamp(desiredAnchoredPos.x, minX, maxX);
        desiredAnchoredPos.y = Mathf.Clamp(desiredAnchoredPos.y, minY, maxY);

        return desiredAnchoredPos;
    }
}
