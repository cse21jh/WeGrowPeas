using TMPro;
using UnityEngine;

/// <summary>
/// 마우스를 따라다니는 범용 설명 툴팁.
///
/// 캔버스마다 하나씩 두고, 그 캔버스 안의 UI가 <see cref="ShowFor"/>로 띄운다.
/// 여러 개가 있으면 마지막에 켜진 것이 쓰이므로 화면당 하나만 두는 것을 전제로 한다.
///
/// 무거운 <see cref="PopupSystem"/> 팝업과 달리 프리팹 풀이나 연출이 없다.
/// 짧은 설명을 커서 옆에 잠깐 보여주는 용도.
/// </summary>
public class HoverTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text text;

    [Tooltip("커서 기준 오프셋. 기본은 오른쪽 아래.")]
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    [Header("가장자리 여백")]
    [Tooltip("이만큼 안쪽까지만 툴팁을 놓는다. 하단 메뉴바처럼 가리면 안 되는 UI가 있을 때 늘린다.\n" +
             "여기에 걸리면 툴팁이 커서 반대편으로 뒤집힌다.")]
    [SerializeField] private float insetBottom = 0f;
    [SerializeField] private float insetTop = 0f;
    [SerializeField] private float insetLeft = 0f;
    [SerializeField] private float insetRight = 0f;

    private Canvas _canvas;
    private RectTransform _canvasRect;

    /// <summary>가장 최근에 활성화된 툴팁. 화면 어디서든 이걸 통해 띄운다.</summary>
    public static HoverTooltip Current { get; private set; }

    public bool IsVisible => panel != null && panel.gameObject.activeSelf;

    /// <summary>툴팁이 있으면 띄운다. 없으면 조용히 넘어간다(연출용이라 없어도 게임은 돈다).</summary>
    public static void ShowFor(string content)
    {
        if (Current != null) Current.Show(content);
    }

    public static void HideCurrent()
    {
        if (Current != null) Current.Hide();
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvasRect = _canvas.transform as RectTransform;

        Hide();
    }

    private void OnEnable() => Current = this;

    private void OnDisable()
    {
        if (Current == this) Current = null;
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

        // 툴팁이 놓일 수 있는 범위. 가장자리 여백만큼 좁아진다.
        float left = -halfW + insetLeft;
        float right = halfW - insetRight;
        float bottom = -halfH + insetBottom;
        float top = halfH - insetTop;

        // 아래(오른쪽)에 자리가 모자라면 커서 반대편으로 뒤집는다.
        // 가장자리에 눌러 붙어 가려지는 것보다 반대쪽에 온전히 뜨는 편이 낫다.
        float x = local.x + offset.x;
        if (x + size.x > right) x = local.x - offset.x - size.x;

        float y = local.y + offset.y; // y는 위쪽 변
        if (y - size.y < bottom) y = local.y - offset.y + size.y;

        // 뒤집어도 넘치면(툴팁이 범위보다 클 때) 마지막으로 안쪽에 붙인다.
        x = Mathf.Clamp(x, left, Mathf.Max(left, right - size.x));
        y = Mathf.Clamp(y, Mathf.Min(top, bottom + size.y), top);

        panel.anchoredPosition = new Vector2(x, y);
    }
}
