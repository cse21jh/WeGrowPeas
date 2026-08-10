using UnityEngine;
using UnityEngine.XR;

public class UILookController : MonoBehaviour
{
    private RectTransform target;
    private Canvas canvas;

    [SerializeField] private Vector2 moveRange = Vector2.zero;
    [SerializeField] private Vector2 originPos = Vector2.zero;

    [SerializeField] private float minDistance = 0.1f; // 최소 거리 임계값

    private void Awake()
    {
        target = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        LookTowardMouse();
    }

    private void LookTowardMouse()
    {
        /*
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mousePos);

        // 마우스 - 타겟 거리 (local 좌표 기준)
        Vector2 dir = mousePos - (Vector2)target.localPosition;

        // 거리 제한
        dir = Vector2.ClampMagnitude(dir, Mathf.Max(moveRange.x, moveRange.y));

        // dead zone 처리 (너무 가까우면 0으로)
        //if (Mathf.Abs(dir.x) < minDistance) dir.x = 0f;
        //if (Mathf.Abs(dir.y) < minDistance) dir.y = 0f;

        // 최종 위치 적용
        target.anchoredPosition = originPos + dir;
        */


        Vector2 mousePos = Input.mousePosition;
        Vector2 targetPos = (Vector2)target.position;

        Vector2 dir = mousePos - targetPos; // 월드 좌표 기준 방향 벡터 계산
        dir = Vector2.ClampMagnitude(dir, Mathf.Max(moveRange.x, moveRange.y)); // 거리 제한

        if (Mathf.Abs(dir.x) < minDistance) dir.x = 0f;
        if (Mathf.Abs(dir.y) < minDistance) dir.y = 0f;

        target.localPosition = (Vector2)originPos + dir; // 최종 위치 적용
    }
}
