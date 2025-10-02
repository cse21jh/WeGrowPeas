using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragPanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform handleArea;  // 드래그 핸들 (예: 상단 바)

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 pointerOffset;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(handleArea, eventData.position, eventData.pressEventCamera))
        {
            isDragging = false;
            return;
        }

        isDragging = true;

        // 클릭 지점과 패널 pivot 위치 사이의 오프셋 저장
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pointerOffset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return; // 드래그 중이 아닐 때 무시

        RectTransform canvasRect = canvas.transform as RectTransform;

        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPos))
        {
            // 마우스 좌표를 캔버스 범위 내로 Clamp
            float clampedX = Mathf.Clamp(localPointerPos.x,
                -canvasRect.rect.width * 0.5f,
                 canvasRect.rect.width * 0.5f);
            float clampedY = Mathf.Clamp(localPointerPos.y,
                -canvasRect.rect.height * 0.5f,
                 canvasRect.rect.height * 0.5f);

            rectTransform.localPosition = new Vector2(clampedX, clampedY) - pointerOffset;
        }
    }
}
