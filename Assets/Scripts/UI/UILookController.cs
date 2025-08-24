using UnityEngine;
using UnityEngine.XR;

public class UILookController : MonoBehaviour
{
    private RectTransform target;
    private Canvas canvas;

    [SerializeField] private Vector2 moveRange = Vector2.zero;
    [SerializeField] private Vector2 originPos = Vector2.zero;


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
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.worldCamera, // Overlay면 null 가능
            out mousePos);

        // 현재 UI 오브젝트의 로컬 좌표
        Vector2 dir = mousePos - (Vector2)target.localPosition;

        dir.Normalize();
        dir = new Vector2(dir.x * moveRange.x, dir.y * moveRange.y);

        target.anchoredPosition = originPos + dir;
    }
}
