using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 퀘스트 항목의 접힘/펼침 상태를 관리한다.
///
/// VerticalLayoutGroup이 직접 관리하는 프리팹 루트의
/// LayoutElement.preferredHeight를 DOTween으로 변경한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LayoutElement))]
public sealed class ExpandableQuestTab : MonoBehaviour
{
    [Header("Layout References")]

    [Tooltip("현재 퀘스트 프리팹 루트에 부착된 LayoutElement입니다.")]
    [SerializeField]
    private LayoutElement rootLayoutElement;

    [Tooltip(
        "퀘스트 목록의 Content RectTransform입니다. " +
        "VerticalLayoutGroup과 ContentSizeFitter가 붙은 오브젝트를 연결합니다."
    )]
    [SerializeField]
    private RectTransform listContent;

    [Header("Description")]

    [Tooltip(
        "상세 설명 전체를 담고 있는 오브젝트입니다. " +
        "접힌 상태에서는 비활성화됩니다."
    )]
    [SerializeField]
    private GameObject descriptionRoot;

    [Tooltip(
        "상세 설명을 페이드 처리할 CanvasGroup입니다. " +
        "필수는 아니며, 비워두면 높이만 애니메이션됩니다."
    )]
    [SerializeField]
    private CanvasGroup descriptionCanvasGroup;

    [Tooltip(
        "상세 설명을 켜고 끄는 버튼의 텍스트메쉬프로 컴포넌트입니다. " +
        "접힌 상태에서는 '상세 설명', 열린 상태에서는 '닫기'로 표시됩니다."
    )]
    [SerializeField]
    private TextMeshProUGUI descriptionBtn;

    [Header("Height")]

    [Tooltip("상세 설명을 닫았을 때의 항목 높이입니다.")]
    [Min(0f)]
    [SerializeField]
    private float collapsedHeight = 27.5f;

    [Tooltip("상세 설명을 열었을 때의 항목 높이입니다.")]
    [Min(0f)]
    [SerializeField]
    private float expandedHeight = 60f;

    [Header("Animation")]

    [Min(0f)]
    [SerializeField]
    private float animationDuration = 0.25f;

    [SerializeField]
    private Ease animationEase = Ease.OutCubic;

    [Tooltip(
        "Time.timeScale이 0이어도 UI 애니메이션이 재생되게 합니다."
    )]
    [SerializeField]
    private bool useUnscaledTime = true;

    [Header("Initial State")]

    [SerializeField]
    private bool initiallyExpanded;

    private Sequence _animationSequence;
    private bool _isExpanded;

    /// <summary>
    /// 현재 상세 설명이 열려 있는지 여부입니다.
    /// </summary>
    public bool IsExpanded => _isExpanded;

    private void Reset()
    {
        rootLayoutElement = GetComponent<LayoutElement>();

        if (transform.parent is RectTransform parentRect)
        {
            listContent = parentRect;
        }
    }

    private void Awake()
    {
        CacheReferences();

        _isExpanded = initiallyExpanded;
        ApplyStateImmediately(_isExpanded);
    }

    private void OnDisable()
    {
        KillCurrentAnimation();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();

        if (expandedHeight < collapsedHeight)
        {
            expandedHeight = collapsedHeight;
        }
    }
#endif

    /// <summary>
    /// 현재 상태를 반대로 변경한다.
    /// 상세 설명 버튼의 OnClick에 연결하면 된다.
    /// </summary>
    public void ToggleExpanded()
    {
        SetExpanded(!_isExpanded);
    }

    /// <summary>
    /// 퀘스트 항목을 펼친다.
    /// </summary>
    public void Expand()
    {
        SetExpanded(true);
    }

    /// <summary>
    /// 퀘스트 항목을 접는다.
    /// </summary>
    public void Collapse()
    {
        SetExpanded(false);
    }

    /// <summary>
    /// 지정한 접힘/펼침 상태로 전환한다.
    /// </summary>
    public void SetExpanded(bool expanded)
    {
        if (_isExpanded == expanded && _animationSequence == null)
        {
            return;
        }

        CacheReferences();

        if (rootLayoutElement == null)
        {
            Debug.LogError(
                $"{name}: LayoutElement가 연결되어 있지 않습니다.",
                this
            );

            return;
        }

        _isExpanded = expanded;

        KillCurrentAnimation();

        float targetHeight = expanded
            ? expandedHeight
            : collapsedHeight;

        if (expanded && descriptionRoot != null)
        {
            // 높이가 늘어나기 전에 설명 오브젝트를 활성화한다.
            descriptionRoot.SetActive(true);
        }

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.interactable = false;
            descriptionCanvasGroup.blocksRaycasts = false;
        }

        _animationSequence = DOTween.Sequence();

        Tween heightTween = DOTween.To(
            () => rootLayoutElement.preferredHeight,
            currentHeight =>
            {
                rootLayoutElement.preferredHeight = currentHeight;

                /*
                 * LayoutElement 변경 내용을 VerticalLayoutGroup과
                 * ContentSizeFitter가 다음 레이아웃 패스에서 반영하도록 한다.
                 */
                RequestLayoutRebuild();
            },
            targetHeight,
            animationDuration
        );

        heightTween.SetEase(animationEase);

        _animationSequence.Append(heightTween);

        if (descriptionCanvasGroup != null)
        {
            float targetAlpha = expanded ? 1f : 0f;

            Tween fadeTween = descriptionCanvasGroup
                .DOFade(targetAlpha, animationDuration * 0.8f)
                .SetEase(Ease.OutQuad);

            if (expanded)
            {
                /*
                 * 펼칠 때는 설명이 약간 늦게 나타나게 한다.
                 */
                fadeTween.SetDelay(animationDuration * 0.15f);
            }

            _animationSequence.Join(fadeTween);
        }

        _animationSequence
            .SetUpdate(useUnscaledTime)
            .OnComplete(() =>
            {
                rootLayoutElement.preferredHeight = targetHeight;

                if (descriptionCanvasGroup != null)
                {
                    descriptionCanvasGroup.alpha = expanded ? 1f : 0f;
                    descriptionCanvasGroup.interactable = expanded;
                    descriptionCanvasGroup.blocksRaycasts = expanded;
                }

                /*
                 * 접기가 끝난 뒤에만 상세 설명을 비활성화한다.
                 * 애니메이션 중 미리 꺼져서 깜빡이는 것을 방지한다.
                 */
                if (!expanded && descriptionRoot != null)
                {
                    descriptionRoot.SetActive(false);
                }

                RequestLayoutRebuild();
                ForceFinalLayoutRebuild();

                _animationSequence = null;
            });

        descriptionBtn.text = expanded ? "닫기" : "상세 설명";
    }

    /// <summary>
    /// 애니메이션 없이 즉시 상태를 지정한다.
    /// 오브젝트 풀링이나 목록 초기화 때 사용할 수 있다.
    /// </summary>
    public void SetExpandedImmediately(bool expanded)
    {
        _isExpanded = expanded;
        ApplyStateImmediately(expanded);
    }

    private void ApplyStateImmediately(bool expanded)
    {
        CacheReferences();
        KillCurrentAnimation();

        if (rootLayoutElement == null)
        {
            return;
        }

        rootLayoutElement.preferredHeight = expanded
            ? expandedHeight
            : collapsedHeight;

        if (descriptionRoot != null)
        {
            descriptionRoot.SetActive(expanded);
        }

        if (descriptionCanvasGroup != null)
        {
            descriptionCanvasGroup.alpha = expanded ? 1f : 0f;
            descriptionCanvasGroup.interactable = expanded;
            descriptionCanvasGroup.blocksRaycasts = expanded;
        }

        RequestLayoutRebuild();
        ForceFinalLayoutRebuild();
    }

    private void CacheReferences()
    {
        if (rootLayoutElement == null)
        {
            rootLayoutElement = GetComponent<LayoutElement>();
        }

        if (listContent == null &&
            transform.parent is RectTransform parentRect)
        {
            listContent = parentRect;
        }
    }

    private void RequestLayoutRebuild()
    {
        /*
         * preferredHeight가 변경되면 LayoutElement도 자체적으로
         * 레이아웃 갱신을 요청하지만, 중첩된 ContentSizeFitter까지
         * 확실히 갱신되도록 목록 Content도 명시적으로 표시한다.
         */
        if (transform is RectTransform ownRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(ownRect);
        }

        if (listContent != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(listContent);
        }
    }

    private void ForceFinalLayoutRebuild()
    {
        /*
         * 매 프레임 강제 갱신하면 비용이 커질 수 있으므로
         * 애니메이션 종료 시점에만 즉시 갱신한다.
         */
        if (listContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }
    }

    private void KillCurrentAnimation()
    {
        if (_animationSequence == null)
        {
            return;
        }

        _animationSequence.Kill();
        _animationSequence = null;
    }
}
