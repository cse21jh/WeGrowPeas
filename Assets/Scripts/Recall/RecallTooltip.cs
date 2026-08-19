using TMPro;
using UnityEngine;

/// <summary>
/// 마우스를 따라다니는 설명 툴팁. 회상 화면 어디서든 같은 것을 쓴다.
///
/// 화면 밖으로 나가지 않도록 캔버스 안쪽으로 붙잡아 둔다.
/// 하단 고정 칸이 아니라 커서 옆에 뜨므로 어떤 아이콘의 설명인지 바로 알 수 있다.
/// </summary>
public class RecallTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text text;

    [Tooltip("커서 기준 오프셋. 기본은 오른쪽 아래.")]
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private Canvas _canvas;
    private RectTransform _canvasRect;

    public bool IsVisible => panel != null && panel.gameObject.activeSelf;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvasRect = _canvas.transform as RectTransform;

        Hide();
    }

    public void Show(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            Hide();
            return;
        }

        if (text != null) text.text = content;
        if (panel != null) panel.gameObject.SetActive(true);

        // 글자가 바뀌면 크기도 바뀐다. 자리를 잡기 전에 확정시킨다.
        Canvas.ForceUpdateCanvases();
        Follow();
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (IsVisible) Follow();
    }

    private void Follow()
    {
        if (panel == null || _canvasRect == null) return;

        // Screen Space - Overlay면 카메라가 null이어야 한다.
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, cam, out Vector2 local))
            return;

        // pivot이 좌상단이라 기본은 커서의 오른쪽 아래로 펼쳐진다.
        Vector2 size = panel.rect.size;
        float halfW = _canvasRect.rect.width * 0.5f;
        float halfH = _canvasRect.rect.height * 0.5f;

        // 아래(오른쪽)에 자리가 모자라면 커서 반대편으로 뒤집는다.
        float x = local.x + offset.x;
        if (x + size.x > halfW) x = local.x - offset.x - size.x;

        float y = local.y + offset.y; // y는 위쪽 변
        if (y - size.y < -halfH) y = local.y - offset.y + size.y;

        // 뒤집어도 넘치면(툴팁이 화면보다 클 때) 마지막으로 안쪽에 붙인다.
        x = Mathf.Clamp(x, -halfW, Mathf.Max(-halfW, halfW - size.x));
        y = Mathf.Clamp(y, Mathf.Min(halfH, -halfH + size.y), halfH);

        panel.anchoredPosition = new Vector2(x, y);
    }
}
