using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 메뉴 버튼 하나를 표현한다.
/// 실제 이동과 선택 연출은 BottomMenuCarousel이 담당한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BottomMenuItemView : MonoBehaviour
{
    [Header("Button")]
    [SerializeField]
    private Button button;

    [Header("Animation Targets")]

    [Tooltip("확대할 RectTransform. 비워두면 버튼 루트 전체가 확대됩니다.")]
    [SerializeField]
    private RectTransform scaleTarget;

    [Tooltip("선택 여부를 표현하는 Tint 오브젝트의 CanvasGroup")]
    [SerializeField]
    private CanvasGroup tintCanvasGroup;

    private Action<BottomMenuItemView> _clickCallback;

    /// <summary>
    /// 캐러셀에서 실제로 이동시킬 버튼 루트 RectTransform.
    /// </summary>
    public RectTransform MoveTarget
    {
        get { return (RectTransform)transform; }
    }

    /// <summary>
    /// 선택 시 확대할 대상.
    /// </summary>
    public RectTransform ScaleTarget
    {
        get
        {
            return scaleTarget != null
                ? scaleTarget
                : MoveTarget;
        }
    }

    public CanvasGroup TintCanvasGroup
    {
        get { return tintCanvasGroup; }
    }

    private void Awake()
    {
        EnsureReferences();
    }

    /// <summary>
    /// 캐러셀에서 버튼 클릭 이벤트를 연결한다.
    /// </summary>
    public void Bind(Action<BottomMenuItemView> clickCallback)
    {
        EnsureReferences();

        _clickCallback = clickCallback;

        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        _clickCallback?.Invoke(this);
    }

    private void EnsureReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            Debug.LogError(
                $"{name}: BottomMenuItemView에 Button이 없습니다.",
                this);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }
}
