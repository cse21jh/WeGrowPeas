using DG.Tweening;
using UnityEngine;

/// <summary>
/// 선택된 하단 메뉴 인덱스에 맞춰 페이지를 활성화한다.
/// </summary>
public sealed class PhoneMenuPageSwitcher : MonoBehaviour
{
    [Tooltip(
        "BottomMenuCarousel의 Items와 동일한 순서로 페이지를 등록합니다.")]
    [SerializeField]
    private RectTransform[] pages;
    [SerializeField] private int currentPageIndex = 2;

    [Space(10)]
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.InOutSine;

    [Tooltip(
        "슬라이드 이동 거리의 기준이 되는 영역입니다. " +
        "비워두면 페이지의 부모를 사용합니다.")]
    [SerializeField]
    private RectTransform viewport;

    [Space(10)]
    [Tooltip(
        "전환 호출과 페이지 활성 상태를 콘솔에 남깁니다.")]
    [SerializeField]
    private bool logTransitions;

    private bool _initialized;

    /// <summary>
    /// UnityEvent&lt;int&gt;에 연결할 페이지 전환 함수.
    /// </summary>
    public void ShowPage(int pageIndex)
    {
        if (!IsValidIndex(pageIndex))
        {
            Debug.LogWarning(
                $"잘못된 페이지 인덱스입니다: {pageIndex}",
                this);

            return;
        }

        if (logTransitions)
        {
            Debug.Log(
                $"[PageSwitcher] {currentPageIndex} -> {pageIndex} / {DescribeActivePages()}",
                this);
        }

        /*
         * 첫 호출과 같은 페이지 재선택은 애니메이션 없이 상태만 맞춘다.
         * 시작 시점에 여러 페이지가 켜져 있어도 여기서 한 번에 정리된다.
         */
        if (!_initialized || pageIndex == currentPageIndex)
        {
            ApplyImmediate(pageIndex);
            return;
        }

        RectTransform target = pages[pageIndex];

        if (target == null)
        {
            Debug.LogWarning(
                $"페이지가 할당되지 않았습니다: {pageIndex}",
                this);
            return;
        }

        RectTransform bounds = viewport != null
            ? viewport
            : target.parent as RectTransform;

        if (bounds == null)
        {
            Debug.LogWarning(
                "슬라이드 기준 영역을 찾을 수 없습니다. Viewport를 지정하세요.",
                this);
            return;
        }

        bool goingRight = currentPageIndex < pageIndex;
        float slideDistance = bounds.rect.width;

        Vector2 enterFrom = new Vector2(
            goingRight ? slideDistance : -slideDistance,
            0f);
        Vector2 exitTo = new Vector2(
            goingRight ? -slideDistance : slideDistance,
            0f);

        currentPageIndex = pageIndex;

        /*
         * Sequence를 쓰지 않는다.
         * Sequence에 중첩된 트윈은 DOKill로 개별 정리가 불가능해서,
         * 전환이 겹치면 이전 OnComplete가 살아남아 활성 상태가 어긋난다.
         */
        target.DOKill();
        target.anchoredPosition = enterFrom;
        target.gameObject.SetActive(true);
        target.DOAnchorPos(Vector2.zero, transitionDuration)
            .SetEase(transitionEase)
            .SetLink(target.gameObject)
            .OnComplete(() => LogScreenState(bounds, $"완료 {pageIndex}"));

        LogScreenState(bounds, $"시작 {pageIndex}");

        for (int i = 0; i < pages.Length; i++)
        {
            RectTransform page = pages[i];

            if (i == pageIndex || page == null || !page.gameObject.activeSelf)
            {
                continue;
            }

            page.DOKill();
            page.DOAnchorPos(exitTo, transitionDuration)
                .SetEase(transitionEase)
                .SetLink(page.gameObject)
                .OnComplete(() => page.gameObject.SetActive(false));
        }
    }

    /// <summary>
    /// 애니메이션 없이 지정한 페이지만 남기고 정리한다.
    /// </summary>
    private void ApplyImmediate(int pageIndex)
    {
        _initialized = true;
        currentPageIndex = pageIndex;

        for (int i = 0; i < pages.Length; i++)
        {
            RectTransform page = pages[i];

            if (page == null)
            {
                continue;
            }

            page.DOKill();
            page.anchoredPosition = Vector2.zero;
            page.gameObject.SetActive(i == pageIndex);
        }
    }

    private bool IsValidIndex(int index)
    {
        return pages != null &&
            index >= 0 &&
            index < pages.Length;
    }

    /// <summary>
    /// 기준 영역의 활성 자식을 전부 나열한다. 진단용.
    /// pages 배열 밖의 오브젝트도 잡아내기 위한 것이다.
    /// </summary>
    private void LogScreenState(
        RectTransform bounds,
        string label)
    {
        if (!logTransitions || bounds == null)
        {
            return;
        }

        string report = string.Empty;

        for (int i = 0; i < bounds.childCount; i++)
        {
            RectTransform child =
                bounds.GetChild(i) as RectTransform;

            if (child == null ||
                !child.gameObject.activeSelf)
            {
                continue;
            }

            report +=
                $"\n  [{i}] {child.name}  pos={child.anchoredPosition}";
        }

        Debug.Log(
            $"[PageSwitcher] {label} / 활성 자식:{report}",
            this);
    }

    private string DescribeActivePages()
    {
        string active = string.Empty;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null &&
                pages[i].gameObject.activeSelf)
            {
                active += active.Length > 0 ? $", {i}" : $"{i}";
            }
        }

        return $"active=[{active}]";
    }
}
