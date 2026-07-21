using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 휴대폰 하단 메뉴를 관리한다.
///
/// Carousel:
/// 선택한 버튼이 중앙으로 이동하는 순환 캐러셀 방식.
///
/// FixedHighlight:
/// 메뉴 순서는 유지하면서 선택한 버튼만 확대·상승·Tint 처리한다.
/// 선택 버튼의 크기에 따라 전체 버튼 간격도 다시 계산한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BottomMenuCarousel : MonoBehaviour
{
    public enum MenuBehaviorMode
    {
        /// <summary>
        /// 선택 버튼이 중앙으로 이동하는 기존 캐러셀 방식.
        /// </summary>
        Carousel,

        /// <summary>
        /// 버튼 순서는 고정되고 선택 버튼만 강조되는 방식.
        /// </summary>
        FixedHighlight
    }

    [Serializable]
    public sealed class MenuSelectedEvent : UnityEvent<int>
    {
    }

    #region Inspector Fields

    [Header("Behavior")]

    [Tooltip(
        "Carousel: 선택 버튼이 중앙으로 이동합니다.\n" +
        "Fixed Highlight: 순서는 고정되고 선택 버튼만 강조됩니다.")]
    [SerializeField]
    private MenuBehaviorMode behaviorMode =
        MenuBehaviorMode.FixedHighlight;

    [Header("Menu Items")]

    [Tooltip("왼쪽에서 오른쪽으로 표시할 순서대로 등록합니다.")]
    [SerializeField]
    private List<BottomMenuItemView> items =
        new List<BottomMenuItemView>();

    [Tooltip("게임 시작 시 처음 선택할 메뉴 인덱스")]
    [SerializeField]
    private int initialSelectedIndex = 2;

    [Header("Automatic Layout")]

    [Tooltip("버튼 부모 영역 왼쪽 끝에서 유지할 여백")]
    [Min(0f)]
    [SerializeField]
    private float leftEdgeOffset = 10f;

    [Tooltip("버튼 부모 영역 오른쪽 끝에서 유지할 여백")]
    [Min(0f)]
    [SerializeField]
    private float rightEdgeOffset = 10f;

    [Tooltip(
        "버튼 사이에서 확보하고 싶은 최소 여백입니다.\n" +
        "공간이 부족하면 실제 간격이 이 값보다 작아질 수 있습니다.")]
    [Min(0f)]
    [SerializeField]
    private float minimumItemGap = 4f;

    [Header("Carousel Layout")]

    [Tooltip(
        "Carousel 모드에서 중앙 버튼과 양옆 버튼 사이에 " +
        "추가로 확보할 간격입니다.")]
    [Min(0f)]
    [SerializeField]
    private float extraCenterGap = 4f;

    [Tooltip(
        "Carousel 모드에서 선택 버튼이 커지는 만큼 " +
        "중앙 양옆 간격을 자동으로 보정합니다.")]
    [SerializeField]
    private bool compensateSelectedScale = true;

    [Header("Carousel Animation")]

    [Tooltip("Carousel 모드에서 버튼이 중앙으로 이동하는 시간")]
    [Min(0.01f)]
    [SerializeField]
    private float moveDuration = 0.45f;

    [SerializeField]
    private Ease moveEase = Ease.OutCubic;

    [Header("Fixed Highlight Animation")]

    [Tooltip(
        "Fixed Highlight 모드에서 버튼 강조 및 재배치가 완료되는 시간")]
    [Min(0f)]
    [SerializeField]
    private float highlightDuration = 0.25f;

    [SerializeField]
    private Ease highlightEase = Ease.OutCubic;

    [Header("Animation Common")]

    [Tooltip(
        "Time.timeScale이 0이어도 메뉴 애니메이션을 실행합니다.")]
    [SerializeField]
    private bool useUnscaledTime = true;

    [Header("Selected Visual")]

    [Tooltip("선택되지 않은 버튼 배율")]
    [Min(0.01f)]
    [SerializeField]
    private float normalScale = 1f;

    [Tooltip("선택된 버튼 배율")]
    [Min(0.01f)]
    [SerializeField]
    private float selectedScale = 1.22f;

    [Tooltip(
        "Carousel 모드에서 중앙으로부터 몇 칸 이내에서 " +
        "확대 및 상승 효과를 적용할지 설정합니다.")]
    [Range(0.1f, 1.5f)]
    [SerializeField]
    private float centerEffectRange = 0.55f;

    [Tooltip("선택된 버튼이 위로 올라가는 거리")]
    [SerializeField]
    private float selectedYOffset = 8f;

    [Header("Tint")]

    [Tooltip("선택되지 않은 버튼 Tint의 알파값")]
    [Range(0f, 1f)]
    [SerializeField]
    private float normalTintAlpha = 0.5f;

    [Tooltip("선택된 버튼 Tint의 알파값")]
    [Range(0f, 1f)]
    [SerializeField]
    private float selectedTintAlpha = 0f;

    [Header("Events")]

    [Tooltip(
        "선택 메뉴가 변경되면 호출됩니다.\n" +
        "인자는 Items 리스트에서의 인덱스입니다.")]
    [SerializeField]
    private MenuSelectedEvent onSelectionChanged =
        new MenuSelectedEvent();

    #endregion

    #region Shared Runtime Fields

    /// <summary>
    /// 각 메뉴 버튼의 선택되지 않은 상태 Y 좌표.
    /// </summary>
    private readonly List<float> _baseYPositions =
        new List<float>();

    /// <summary>
    /// 각 메뉴 버튼의 원래 Local Scale.
    /// </summary>
    private readonly List<Vector3> _baseScales =
        new List<Vector3>();

    /// <summary>
    /// Fixed Highlight 모드에서 계산된 각 버튼의 X 좌표.
    /// </summary>
    private readonly List<float> _fixedXPositions =
        new List<float>();

    private RectTransform _layoutRect;

    private int _currentIndex;
    private bool _initialized;

    private Coroutine _refreshAfterEnableCoroutine;

    #endregion

    #region Carousel Runtime Fields

    private Tween _moveTween;

    private int _queuedIndex = -1;
    private float _scrollSlots;

    /// <summary>
    /// 캐러셀 일반 버튼 중심 간격.
    /// </summary>
    private float _baseSpacing;

    /// <summary>
    /// 캐러셀 중앙과 바로 옆 버튼 사이의 추가 간격.
    /// </summary>
    private float _calculatedCenterGap;

    /// <summary>
    /// 좌우 Edge Offset을 반영한 캐러셀 중앙 X 좌표.
    /// </summary>
    private float _layoutCenterX;

    private bool _isMoving;

    #endregion

    #region Fixed Highlight Runtime Fields

    private Sequence _highlightSequence;

    #endregion

    #region Public Properties

    public int CurrentIndex => _currentIndex;

    public MenuBehaviorMode Mode
    {
        get => behaviorMode;
        set => SetBehaviorMode(value);
    }

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        /*
         * 최초 활성화 시에는 Start()에서 Initialize()가 실행된다.
         * 이미 초기화된 뒤 다시 활성화되는 경우에만 복원 코루틴을 실행한다.
         */
        if (!Application.isPlaying ||
            !_initialized)
        {
            return;
        }

        if (_refreshAfterEnableCoroutine != null)
        {
            StopCoroutine(_refreshAfterEnableCoroutine);
        }

        _refreshAfterEnableCoroutine =
            StartCoroutine(RefreshAfterEnable());
    }

    private void Start()
    {
        Initialize();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!_initialized)
        {
            return;
        }

        RecalculateLayout();

        switch (behaviorMode)
        {
            case MenuBehaviorMode.Carousel:
                ApplyCarouselVisuals();
                break;

            case MenuBehaviorMode.FixedHighlight:
                ApplyFixedVisualsImmediately();
                break;
        }
    }

    private void OnDisable()
    {
        /*
         * 부모가 비활성화되는 도중에는
         * SetAsLastSibling, RectTransform 재배치 등을 실행하면 안 된다.
         */

        if (_refreshAfterEnableCoroutine != null)
        {
            StopCoroutine(_refreshAfterEnableCoroutine);
            _refreshAfterEnableCoroutine = null;
        }

        KillAllTweens();

        _isMoving = false;
        _queuedIndex = -1;
        _scrollSlots = 0f;

        /*
         * 여기서 ApplyCurrentModeImmediately()나
         * BringSelectedItemToFront()를 호출하지 않는다.
         */
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        if (items == null ||
            items.Count == 0)
        {
            Debug.LogError(
                $"{name}: 등록된 메뉴 버튼이 없습니다.",
                this);

            enabled = false;
            return;
        }

        _layoutRect =
            transform as RectTransform;

        if (_layoutRect == null)
        {
            Debug.LogError(
                $"{name}: BottomMenuCarousel은 " +
                "RectTransform 오브젝트에 붙어 있어야 합니다.",
                this);

            enabled = false;
            return;
        }

        _baseYPositions.Clear();
        _baseScales.Clear();
        _fixedXPositions.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            BottomMenuItemView item =
                items[i];

            if (item == null)
            {
                Debug.LogError(
                    $"{name}: Items의 {i}번 요소가 비어 있습니다.",
                    this);

                enabled = false;
                return;
            }

            item.Bind(OnItemClicked);

            _baseYPositions.Add(
                item.MoveTarget.anchoredPosition.y);

            _baseScales.Add(
                item.ScaleTarget.localScale);

            _fixedXPositions.Add(0f);
        }

        Canvas.ForceUpdateCanvases();

        _currentIndex =
            Mod(
                initialSelectedIndex,
                items.Count);

        _scrollSlots = 0f;
        _initialized = true;

        RecalculateLayout();
        ApplyCurrentModeImmediately();

        /*
         * 초기화는 활성화 완료 후 Start에서 실행되므로
         * 이 시점에는 sibling 변경이 가능하다.
         */
        BringSelectedItemToFront();

        onSelectionChanged.Invoke(
            _currentIndex);
    }

    /// <summary>
    /// 부모 오브젝트 활성화가 완전히 끝난 다음 프레임에
    /// 레이아웃과 sibling 순서를 복구한다.
    /// </summary>
    private IEnumerator RefreshAfterEnable()
    {
        yield return null;

        _refreshAfterEnableCoroutine = null;

        if (!_initialized ||
            !isActiveAndEnabled ||
            !gameObject.activeInHierarchy)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        RecalculateLayout();
        ApplyCurrentModeImmediately();
        BringSelectedItemToFront();
    }

    #endregion

    #region Selection

    private void OnItemClicked(
        BottomMenuItemView clickedItem)
    {
        int targetIndex =
            items.IndexOf(clickedItem);

        if (targetIndex < 0)
        {
            return;
        }

        SelectIndex(targetIndex);
    }

    /// <summary>
    /// 외부 코드에서도 특정 메뉴를 선택할 수 있다.
    /// </summary>
    public void SelectIndex(int targetIndex)
    {
        if (!_initialized ||
            items.Count == 0)
        {
            return;
        }

        targetIndex =
            Mod(
                targetIndex,
                items.Count);

        switch (behaviorMode)
        {
            case MenuBehaviorMode.Carousel:
                SelectCarouselIndex(
                    targetIndex);
                break;

            case MenuBehaviorMode.FixedHighlight:
                SelectFixedIndex(
                    targetIndex);
                break;
        }
    }

    /// <summary>
    /// 런타임에서 메뉴 작동 방식을 변경한다.
    /// </summary>
    public void SetBehaviorMode(
        MenuBehaviorMode newMode)
    {
        if (behaviorMode == newMode)
        {
            return;
        }

        KillAllTweens();

        behaviorMode = newMode;

        _scrollSlots = 0f;
        _isMoving = false;
        _queuedIndex = -1;

        if (!_initialized)
        {
            return;
        }

        RecalculateLayout();
        ApplyCurrentModeImmediately();
        BringSelectedItemToFront();
    }

    /// <summary>
    /// 현재 부모 크기와 설정값을 기준으로 레이아웃을 다시 계산한다.
    /// </summary>
    [ContextMenu("Refresh Layout")]
    public void RefreshLayout()
    {
        if (!_initialized)
        {
            return;
        }

        KillAllTweens();

        RecalculateLayout();
        ApplyCurrentModeImmediately();
        BringSelectedItemToFront();
    }

    #endregion

    #region Shared Layout

    private void RecalculateLayout()
    {
        if (!_initialized ||
            _layoutRect == null ||
            items.Count == 0)
        {
            return;
        }

        switch (behaviorMode)
        {
            case MenuBehaviorMode.Carousel:
                RecalculateCarouselLayout();
                break;

            case MenuBehaviorMode.FixedHighlight:
                RecalculateFixedLayout();
                break;
        }
    }

    /// <summary>
    /// 현재 모드의 시각 상태를 애니메이션 없이 즉시 적용한다.
    ///
    /// sibling 순서는 이 함수에서 변경하지 않는다.
    /// 활성화·비활성화 도중 호출될 가능성을 분리하기 위해서다.
    /// </summary>
    private void ApplyCurrentModeImmediately()
    {
        if (!_initialized)
        {
            return;
        }

        switch (behaviorMode)
        {
            case MenuBehaviorMode.Carousel:
                _scrollSlots = 0f;
                ApplyCarouselVisuals();
                break;

            case MenuBehaviorMode.FixedHighlight:
                ApplyFixedVisualsImmediately();
                break;
        }
    }

    #endregion

    #region Fixed Highlight Mode

    /// <summary>
    /// 버튼 순서는 유지하고 선택 강조만 변경한다.
    /// 선택 버튼의 확대된 크기에 맞춰 전체 X 위치도 다시 계산한다.
    /// </summary>
    private void SelectFixedIndex(
        int targetIndex)
    {
        if (targetIndex == _currentIndex)
        {
            return;
        }

        _currentIndex = targetIndex;

        /*
         * 새 선택 버튼의 확대 크기를 반영해
         * 전체 버튼 간격을 다시 계산한다.
         */
        RecalculateFixedLayout();

        BringSelectedItemToFront();
        AnimateFixedVisuals();

        onSelectionChanged.Invoke(
            _currentIndex);
    }

    /// <summary>
    /// 현재 선택 상태를 반영해 Fixed Highlight 버튼 위치를 계산한다.
    ///
    /// 좌우 Edge Offset은 유지하며,
    /// 버튼들이 차지하고 남은 공간을 각 버튼 사이에 동일하게 분배한다.
    /// </summary>
    private void RecalculateFixedLayout()
    {
        int count =
            items.Count;

        if (count <= 0)
        {
            return;
        }

        EnsureFixedPositionListSize(
            count);

        float leftBoundary =
            _layoutRect.rect.xMin +
            leftEdgeOffset;

        float rightBoundary =
            _layoutRect.rect.xMax -
            rightEdgeOffset;

        float availableWidth =
            rightBoundary -
            leftBoundary;

        if (availableWidth <= 0f)
        {
            Debug.LogWarning(
                $"{name}: 좌우 Edge Offset을 제외한 " +
                "배치 영역이 없습니다.",
                this);

            float centerX =
                _layoutRect.rect.center.x;

            for (int i = 0; i < count; i++)
            {
                _fixedXPositions[i] =
                    centerX;
            }

            return;
        }

        float[] currentWidths =
            new float[count];

        float totalItemWidth = 0f;

        for (int i = 0; i < count; i++)
        {
            float scaleMultiplier =
                i == _currentIndex
                    ? selectedScale
                    : normalScale;

            currentWidths[i] =
                GetItemBaseWidth(i) *
                scaleMultiplier;

            totalItemWidth +=
                currentWidths[i];
        }

        if (count == 1)
        {
            _fixedXPositions[0] =
                (leftBoundary +
                 rightBoundary) *
                0.5f;

            return;
        }

        /*
         * 전체 배치 가능 너비에서
         * 현재 표시될 버튼들의 실제 너비 합을 뺀다.
         */
        float remainingWidth =
            availableWidth -
            totalItemWidth;

        /*
         * 남은 공간을 버튼 사이의 간격에 동일하게 분배한다.
         */
        float distributedGap =
            remainingWidth /
            (count - 1);

        if (distributedGap < 0f)
        {
            Debug.LogWarning(
                $"{name}: 버튼 전체 너비가 배치 영역보다 큽니다. " +
                "Selected Scale이나 Edge Offset을 줄여주세요.",
                this);
        }
        else if (distributedGap < minimumItemGap)
        {
            /*
            Debug.LogWarning(
                $"{name}: 현재 버튼 사이 간격은 " +
                $"{distributedGap:F1}이며, Minimum Item Gap " +
                $"{minimumItemGap:F1}보다 작습니다.",
                this);
            */
        }

        /*
         * 첫 번째 버튼의 왼쪽 외곽선이
         * Left Edge Offset 위치에 맞도록 배치한다.
         */
        float currentX =
            leftBoundary +
            currentWidths[0] *
            0.5f;

        _fixedXPositions[0] =
            currentX;

        for (int i = 1; i < count; i++)
        {
            float previousHalfWidth =
                currentWidths[i - 1] *
                0.5f;

            float currentHalfWidth =
                currentWidths[i] *
                0.5f;

            currentX +=
                previousHalfWidth +
                distributedGap +
                currentHalfWidth;

            _fixedXPositions[i] =
                currentX;
        }
    }

    /// <summary>
    /// Fixed Highlight 모드의 현재 상태를 즉시 적용한다.
    /// </summary>
    private void ApplyFixedVisualsImmediately()
    {
        for (int i = 0; i < items.Count; i++)
        {
            bool isSelected =
                i == _currentIndex;

            ApplyFixedItemVisualImmediately(
                i,
                isSelected);
        }
    }

    private void ApplyFixedItemVisualImmediately(
        int itemIndex,
        bool isSelected)
    {
        BottomMenuItemView item =
            items[itemIndex];

        float scaleMultiplier =
            isSelected
                ? selectedScale
                : normalScale;

        Vector3 baseScale =
            _baseScales[itemIndex];

        item.ScaleTarget.localScale =
            new Vector3(
                baseScale.x *
                scaleMultiplier,
                baseScale.y *
                scaleMultiplier,
                baseScale.z);

        Vector2 targetPosition =
            new Vector2(
                _fixedXPositions[itemIndex],
                _baseYPositions[itemIndex] +
                (isSelected
                    ? selectedYOffset
                    : 0f));

        item.MoveTarget.anchoredPosition =
            targetPosition;

        if (item.TintCanvasGroup != null)
        {
            item.TintCanvasGroup.alpha =
                isSelected
                    ? selectedTintAlpha
                    : normalTintAlpha;
        }
    }

    /// <summary>
    /// Fixed Highlight 모드에서 크기, X/Y 위치, Tint를 동시에 변경한다.
    /// </summary>
    private void AnimateFixedVisuals()
    {
        _highlightSequence?.Kill();

        _highlightSequence =
            DOTween.Sequence();

        if (useUnscaledTime)
        {
            _highlightSequence.SetUpdate(true);
        }

        for (int i = 0; i < items.Count; i++)
        {
            BottomMenuItemView item =
                items[i];

            bool isSelected =
                i == _currentIndex;

            float scaleMultiplier =
                isSelected
                    ? selectedScale
                    : normalScale;

            Vector3 baseScale =
                _baseScales[i];

            Vector3 targetScale =
                new Vector3(
                    baseScale.x *
                    scaleMultiplier,
                    baseScale.y *
                    scaleMultiplier,
                    baseScale.z);

            Vector2 targetPosition =
                new Vector2(
                    _fixedXPositions[i],
                    _baseYPositions[i] +
                    (isSelected
                        ? selectedYOffset
                        : 0f));

            float targetTintAlpha =
                isSelected
                    ? selectedTintAlpha
                    : normalTintAlpha;

            _highlightSequence.Join(
                item.ScaleTarget
                    .DOScale(
                        targetScale,
                        highlightDuration)
                    .SetEase(highlightEase));

            /*
             * X 재배치와 선택 버튼의 Y 상승을
             * 동일한 Tween으로 처리한다.
             */
            _highlightSequence.Join(
                item.MoveTarget
                    .DOAnchorPos(
                        targetPosition,
                        highlightDuration)
                    .SetEase(highlightEase));

            if (item.TintCanvasGroup != null)
            {
                _highlightSequence.Join(
                    item.TintCanvasGroup
                        .DOFade(
                            targetTintAlpha,
                            highlightDuration)
                        .SetEase(highlightEase));
            }
        }

        _highlightSequence.OnComplete(
            () =>
            {
                _highlightSequence = null;
            });
    }

    private void EnsureFixedPositionListSize(
        int requiredCount)
    {
        while (_fixedXPositions.Count <
               requiredCount)
        {
            _fixedXPositions.Add(0f);
        }

        if (_fixedXPositions.Count >
            requiredCount)
        {
            _fixedXPositions.RemoveRange(
                requiredCount,
                _fixedXPositions.Count -
                requiredCount);
        }
    }

    #endregion

    #region Carousel Mode

    /// <summary>
    /// 기존 순환 캐러셀 방식으로 특정 메뉴를 선택한다.
    /// </summary>
    private void SelectCarouselIndex(
        int targetIndex)
    {
        if (_isMoving)
        {
            _queuedIndex =
                targetIndex;

            return;
        }

        int moveStep =
            GetShortestMoveStep(
                _currentIndex,
                targetIndex,
                items.Count);

        if (moveStep == 0)
        {
            return;
        }

        _isMoving = true;
        _queuedIndex = -1;

        /*
         * 확대되며 중앙으로 이동할 버튼이
         * 다른 버튼 위에 그려지도록 한다.
         */
        BringItemToFront(
            targetIndex);

        _moveTween?.Kill();

        _moveTween =
            DOVirtual.Float(
                    0f,
                    moveStep,
                    moveDuration,
                    value =>
                    {
                        _scrollSlots =
                            value;

                        ApplyCarouselVisuals();
                    })
                .SetEase(moveEase)
                .OnComplete(
                    () =>
                    {
                        CompleteCarouselMove(
                            moveStep);
                    });

        if (useUnscaledTime)
        {
            _moveTween.SetUpdate(true);
        }
    }

    private void CompleteCarouselMove(
        int moveStep)
    {
        _currentIndex =
            Mod(
                _currentIndex +
                moveStep,
                items.Count);

        _scrollSlots = 0f;
        _isMoving = false;
        _moveTween = null;

        BringSelectedItemToFront();
        ApplyCarouselVisuals();

        onSelectionChanged.Invoke(
            _currentIndex);

        int queuedIndex =
            _queuedIndex;

        _queuedIndex = -1;

        if (queuedIndex >= 0 &&
            queuedIndex != _currentIndex)
        {
            SelectCarouselIndex(
                queuedIndex);
        }
    }

    /// <summary>
    /// 부모 너비, 좌우 Offset, 버튼 너비를 기준으로
    /// 캐러셀 간격을 계산한다.
    /// </summary>
    private void RecalculateCarouselLayout()
    {
        float maxBaseItemWidth =
            GetMaxBaseItemWidth();

        float normalItemWidth =
            maxBaseItemWidth *
            normalScale;

        float selectedItemWidth =
            maxBaseItemWidth *
            selectedScale;

        float minimumCenterX =
            _layoutRect.rect.xMin +
            leftEdgeOffset +
            normalItemWidth *
            0.5f;

        float maximumCenterX =
            _layoutRect.rect.xMax -
            rightEdgeOffset -
            normalItemWidth *
            0.5f;

        if (maximumCenterX <
            minimumCenterX)
        {
            Debug.LogWarning(
                $"{name}: 메뉴 부모 너비가 " +
                "버튼 배치에 비해 너무 작습니다.",
                this);

            maximumCenterX =
                minimumCenterX;
        }

        _layoutCenterX =
            (minimumCenterX +
             maximumCenterX) *
            0.5f;

        float availableHalfWidth =
            (maximumCenterX -
             minimumCenterX) *
            0.5f;

        int maximumSlot =
            Mathf.FloorToInt(
                items.Count *
                0.5f);

        float scaleCompensation = 0f;

        if (compensateSelectedScale)
        {
            scaleCompensation =
                Mathf.Max(
                    0f,
                    (selectedItemWidth -
                     normalItemWidth) *
                    0.5f);
        }

        float requestedCenterGap =
            scaleCompensation +
            extraCenterGap;

        if (maximumSlot <= 0)
        {
            _baseSpacing = 0f;
            _calculatedCenterGap = 0f;
            return;
        }

        float minimumBaseSpacing =
            normalItemWidth +
            minimumItemGap;

        float maximumAllowedCenterGap =
            availableHalfWidth -
            maximumSlot *
            minimumBaseSpacing;

        if (maximumAllowedCenterGap >= 0f)
        {
            _calculatedCenterGap =
                Mathf.Min(
                    requestedCenterGap,
                    maximumAllowedCenterGap);

            if (_calculatedCenterGap <
                requestedCenterGap -
                0.01f)
            {
                Debug.LogWarning(
                    $"{name}: 부모 너비가 부족하여 중앙 추가 간격이 " +
                    $"{requestedCenterGap:F1}에서 " +
                    $"{_calculatedCenterGap:F1}로 제한됐습니다.",
                    this);
            }
        }
        else
        {
            _calculatedCenterGap =
                Mathf.Min(
                    requestedCenterGap,
                    availableHalfWidth);

            Debug.LogWarning(
                $"{name}: 현재 부모 너비로는 " +
                "Minimum Item Gap을 유지할 수 없습니다.",
                this);
        }

        _baseSpacing =
            Mathf.Max(
                0f,
                (availableHalfWidth -
                 _calculatedCenterGap) /
                maximumSlot);
    }

    private void ApplyCarouselVisuals()
    {
        int count =
            items.Count;

        for (int i = 0; i < count; i++)
        {
            BottomMenuItemView item =
                items[i];

            float relativeSlot =
                i -
                _currentIndex -
                _scrollSlots;

            float wrappedSlot =
                WrapCentered(
                    relativeSlot,
                    count);

            float centerWeight =
                GetCenterWeight(
                    wrappedSlot);

            ApplyCarouselPosition(
                item,
                i,
                wrappedSlot,
                centerWeight);

            ApplyCarouselCenterEffect(
                item,
                i,
                centerWeight);
        }
    }

    /// <summary>
    /// 버튼이 캐러셀 중앙에 가까울수록 1을 반환한다.
    /// </summary>
    private float GetCenterWeight(
        float wrappedSlot)
    {
        float weight =
            1f -
            Mathf.Clamp01(
                Mathf.Abs(wrappedSlot) /
                Mathf.Max(
                    0.001f,
                    centerEffectRange));

        return
            weight *
            weight *
            (3f - 2f * weight);
    }

    private void ApplyCarouselPosition(
        BottomMenuItemView item,
        int itemIndex,
        float wrappedSlot,
        float centerWeight)
    {
        Vector2 position =
            item.MoveTarget.anchoredPosition;

        position.x =
            GetCarouselPositionX(
                wrappedSlot);

        position.y =
            _baseYPositions[itemIndex] +
            selectedYOffset *
            centerWeight;

        item.MoveTarget.anchoredPosition =
            position;
    }

    private float GetCarouselPositionX(
        float slot)
    {
        float absoluteSlot =
            Mathf.Abs(slot);

        if (absoluteSlot <=
            Mathf.Epsilon)
        {
            return _layoutCenterX;
        }

        float distance;

        if (absoluteSlot <= 1f)
        {
            distance =
                absoluteSlot *
                (_baseSpacing +
                 _calculatedCenterGap);
        }
        else
        {
            distance =
                absoluteSlot *
                _baseSpacing +
                _calculatedCenterGap;
        }

        return
            _layoutCenterX +
            Mathf.Sign(slot) *
            distance;
    }

    private void ApplyCarouselCenterEffect(
        BottomMenuItemView item,
        int itemIndex,
        float centerWeight)
    {
        float scaleMultiplier =
            Mathf.Lerp(
                normalScale,
                selectedScale,
                centerWeight);

        Vector3 baseScale =
            _baseScales[itemIndex];

        item.ScaleTarget.localScale =
            new Vector3(
                baseScale.x *
                scaleMultiplier,
                baseScale.y *
                scaleMultiplier,
                baseScale.z);

        if (item.TintCanvasGroup != null)
        {
            item.TintCanvasGroup.alpha =
                Mathf.Lerp(
                    normalTintAlpha,
                    selectedTintAlpha,
                    centerWeight);
        }
    }

    private static int GetShortestMoveStep(
        int currentIndex,
        int targetIndex,
        int count)
    {
        int forwardStep =
            Mod(
                targetIndex -
                currentIndex,
                count);

        if (forwardStep == 0)
        {
            return 0;
        }

        int backwardStep =
            forwardStep -
            count;

        return
            Mathf.Abs(forwardStep) <=
            Mathf.Abs(backwardStep)
                ? forwardStep
                : backwardStep;
    }

    private static float WrapCentered(
        float value,
        int count)
    {
        float halfCount =
            count *
            0.5f;

        return
            Mathf.Repeat(
                value +
                halfCount,
                count) -
            halfCount;
    }

    #endregion

    #region Rendering Order

    /// <summary>
    /// 현재 선택된 버튼을 가장 마지막 sibling으로 이동한다.
    /// </summary>
    private void BringSelectedItemToFront()
    {
        BringItemToFront(
            _currentIndex);
    }

    /// <summary>
    /// 지정한 버튼을 가장 마지막 sibling으로 이동한다.
    ///
    /// 부모나 자식이 활성화되지 않은 상태에서는
    /// SetAsLastSibling을 호출하지 않는다.
    /// </summary>
    private void BringItemToFront(
        int itemIndex)
    {
        if (!_initialized ||
            items == null ||
            items.Count == 0)
        {
            return;
        }

        itemIndex =
            Mod(
                itemIndex,
                items.Count);

        BottomMenuItemView item =
            items[itemIndex];

        if (item == null ||
            item.MoveTarget == null)
        {
            return;
        }

        if (!gameObject.activeInHierarchy ||
            !item.gameObject.activeInHierarchy)
        {
            return;
        }

        item.MoveTarget.SetAsLastSibling();
    }

    #endregion

    #region Shared Utility

    private float GetMaxBaseItemWidth()
    {
        float maximumWidth = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            maximumWidth =
                Mathf.Max(
                    maximumWidth,
                    GetItemBaseWidth(i));
        }

        return maximumWidth;
    }

    private float GetItemBaseWidth(
        int itemIndex)
    {
        RectTransform scaleTarget =
            items[itemIndex].ScaleTarget;

        Vector3 baseScale =
            _baseScales[itemIndex];

        return
            scaleTarget.rect.width *
            Mathf.Abs(baseScale.x);
    }

    private static int Mod(
        int value,
        int modulo)
    {
        return
            (value % modulo +
             modulo) %
            modulo;
    }

    private void KillAllTweens()
    {
        _moveTween?.Kill();
        _moveTween = null;

        _highlightSequence?.Kill();
        _highlightSequence = null;
    }

    #endregion
}
