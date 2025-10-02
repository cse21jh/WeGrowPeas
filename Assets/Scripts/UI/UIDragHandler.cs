using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform handleArea;  // 드래그 가능한 영역 (예: 상단바)

    private RectTransform rectTransform;   // 패널 자체
    private Canvas canvas;                 // 최상위 Canvas
    private Vector2 pointerOffset;         // 마우스 클릭 시 패널 안에서의 상대 좌표
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 핸들 영역 안에서만 드래그 시작 허용
        if (!RectTransformUtility.RectangleContainsScreenPoint(handleArea, eventData.position, eventData.pressEventCamera))
        {
            isDragging = false;
            return;
        }

        isDragging = true;

        RectTransform canvasRect = canvas.transform as RectTransform;

        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            pointerOffset = localPointerPos - (Vector2)rectTransform.localPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        RectTransform canvasRect = canvas.transform as RectTransform;

        // 먼저 마우스 위치를 Canvas 화면 크기 안으로 Clamp
        Vector2 clampedScreenPos = eventData.position;

        float minX = 0f;
        float maxX = Screen.width;
        float minY = 0f;
        float maxY = Screen.height;

        clampedScreenPos.x = Mathf.Clamp(clampedScreenPos.x, minX, maxX);
        clampedScreenPos.y = Mathf.Clamp(clampedScreenPos.y, minY, maxY);

        // 이제 Clamp된 마우스 좌표를 이용해서 패널 이동
        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            clampedScreenPos, // ← 제한된 마우스 좌표
            eventData.pressEventCamera,
            out localPointerPos))
        {
            rectTransform.localPosition = localPointerPos - pointerOffset;
        }
    }
}
