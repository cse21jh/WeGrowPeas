using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MandatoryMessagePopupController : MonoBehaviour, IPointerClickHandler
{
    [Header("Popup Animation")]
    [SerializeField] private RectTransform msgBox;
    [SerializeField, Range(0f, 1f)] private float collapsedScaleX;
    [SerializeField, Min(0f)] private float showDuration = 0.45f;
    [SerializeField] private Ease showEase = Ease.OutBack;

    [Header("Popup Contents")]
    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text partnerNameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private CanvasGroup nextButtonCanvasGroup;
    [SerializeField] private Button confirmButton;

    [Header("Typewriter Animation")]
    [SerializeField] private bool animateTyping = true;
    [Tooltip("초당 표시할 글자 수. 한글과 조합 문자는 한 글자 단위로 표시합니다.")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 32f;
    [SerializeField, Min(0f)] private float typingStartDelay = 0.08f;
    [Tooltip("표시된 글자에 맞춰 말풍선 크기가 따라가는 시간. 0이면 즉시 조절합니다.")]
    [SerializeField, Min(0f)] private float resizeSmoothTime = 0.08f;

    [Header("Adaptive Bubble Layout")]
    [SerializeField] private ScrollRect messageScroll;
    [SerializeField] private RectTransform messageViewport;
    [SerializeField, Min(1f)] private float minimumWidth = 160f;
    [SerializeField, Min(1f)] private float maximumWidth = 420f;
    [SerializeField, Min(1f)] private float minimumHeight = 53.333f;
    [SerializeField, Range(0.2f, 0.9f)] private float maximumScreenHeight = 0.6f;
    [Tooltip("본문 여백: 왼쪽, 위, 오른쪽, 아래 (Canvas 단위)")]
    [SerializeField] private Vector4 textPadding = new Vector4(30.833f, 10.417f, 49.167f, 20.833f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(41.25f, 22.5f);
    [SerializeField, Min(0f)] private float screenMargin = 20f;
    [SerializeField, Min(0f)] private float navigationOverhang = 12.5f;
    [SerializeField, Min(0f)] private float bottomMargin = 8f;

    private Action onPrevious;
    private Action onNext;
    private Action onConfirm;
    private Tween showTween;
    private Vector2 shownPosition;
    private Vector2 shownPivot;
    private Vector3 shownScale;
    private bool hasShownTransform;
    private Vector2 lastAvailableSize;
    private string laidOutText;
    private readonly List<int> revealEnds = new List<int>();
    private string fullMessage = string.Empty;
    private int revealedElements;
    private float typingElapsed;
    private bool isTyping;
    private bool hasLayout;
    private bool layoutNeedsApply;
    private bool resetScrollPending;
    private bool scrollToLatest;
    private Vector2 targetBodySize;
    private Vector2 layoutVelocity;
    private float targetTextWidth;
    private float targetTextHeight;

    public bool IsTyping => isTyping;

    public void Initialize(
        Action previous,
        Action next,
        Action confirm)
    {
        onPrevious = previous;
        onNext = next;
        onConfirm = confirm;
        CacheShownTransform();

        previousButton?.onClick.RemoveAllListeners();
        nextButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.RemoveAllListeners();

        previousButton?.onClick.AddListener(HandlePrevious);
        nextButton?.onClick.AddListener(HandleNext);
        confirmButton?.onClick.AddListener(HandleConfirm);
        Hide();
    }

    public void Show(ChatPartner partner, string body, bool animateText = true)
    {
        bool wasOpen = gameObject.activeSelf;
        StopShowAnimation();
        gameObject.SetActive(true);

        if (profileImage != null)
        {
            profileImage.sprite = partner != null ? partner.MessagePopupPortrait : null;
            profileImage.enabled = profileImage.sprite != null;
        }

        if (partnerNameText != null)
            partnerNameText.text = partner != null ? partner.LocalizedName : string.Empty;

        if (!wasOpen) hasLayout = false;
        BeginTyping(body ?? string.Empty, animateText);

        RefreshLayout(true);
        if (!wasOpen)
            PlayShowAnimation();
    }

    public void SetNavigation(bool isFirst, bool isLast, bool waitForSignal, bool isUnlocked)
    {
        if (previousButton != null)
            previousButton.gameObject.SetActive(!isFirst);

        bool showLockedNext = isLast && waitForSignal && !isUnlocked;
        bool showNext = !isLast || showLockedNext;

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(showNext);
            nextButton.interactable = showNext && (!waitForSignal || isUnlocked);
        }

        if (nextButtonCanvasGroup != null)
            nextButtonCanvasGroup.alpha = nextButton != null && nextButton.interactable ? 1f : 0.4f;

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(isLast && !showLockedNext);
    }

    public void Hide()
    {
        StopTyping();
        StopShowAnimation();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopTyping();
        StopShowAnimation();
    }

    private void OnDestroy()
    {
        showTween?.Kill();
    }

    private void LateUpdate()
    {
        if (messageText == null) return;
        bool revealed = UpdateTyping();
        Vector2 available = AvailableSize();
        if (available != lastAvailableSize || laidOutText != messageText.text)
            RefreshLayout(!revealed && laidOutText != messageText.text);

        if (!hasLayout) return;
        Vector2 current = msgBox.rect.size;
        if ((current - targetBodySize).sqrMagnitude > 0.0001f)
        {
            Vector2 size = resizeSmoothTime <= 0f ? targetBodySize : Vector2.SmoothDamp(
                current, targetBodySize, ref layoutVelocity, resizeSmoothTime,
                Mathf.Infinity, Time.unscaledDeltaTime);
            if ((size - targetBodySize).sqrMagnitude < 0.0001f)
            {
                size = targetBodySize;
                layoutVelocity = Vector2.zero;
            }
            ApplyLayout(size);
        }
        else if (layoutNeedsApply)
            ApplyLayout(targetBodySize);
    }

    private void BeginTyping(string body, bool animateText)
    {
        StopTyping();
        fullMessage = body;
        if (messageText == null) return;

        messageText.maxVisibleCharacters = int.MaxValue;
        messageText.text = body;
        if (!Application.isPlaying || !animateTyping || !animateText || body.Length == 0)
            return;

        // TMP supplies source indices after parsing rich-text tags and sprite tokens.
        // Combine them with Unicode text-element boundaries to avoid revealing half a glyph.
        messageText.ForceMeshUpdate(true);
        int[] elements = StringInfo.ParseCombiningCharacters(body);
        var info = messageText.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var character = info.characterInfo[i];
            int end = Mathf.Clamp(character.index + character.stringLength, 0, body.Length);
            int boundary = Array.BinarySearch(elements, end);
            if (boundary < 0)
            {
                boundary = ~boundary;
                end = boundary < elements.Length ? elements[boundary] : body.Length;
            }
            if (end > 0 && (revealEnds.Count == 0 || end > revealEnds[revealEnds.Count - 1]))
                revealEnds.Add(end);
        }
        if (revealEnds.Count == 0) return;
        revealEnds[revealEnds.Count - 1] = body.Length;
        messageText.text = string.Empty;
        typingElapsed = -typingStartDelay;
        revealedElements = 0;
        isTyping = true;
    }

    private bool UpdateTyping()
    {
        if (!isTyping) return false;
        typingElapsed += Time.unscaledDeltaTime;
        if (typingElapsed < 0f) return false;
        int count = Mathf.Min(revealEnds.Count,
            Mathf.FloorToInt(typingElapsed * Mathf.Max(1f, charactersPerSecond)) + 1);
        if (count == revealedElements) return false;
        revealedElements = count;
        messageText.text = count == revealEnds.Count
            ? fullMessage : fullMessage.Substring(0, revealEnds[count - 1]);
        isTyping = count < revealEnds.Count;
        scrollToLatest = true;
        return true;
    }

    /// <summary>첫 클릭은 현재 문장을 완성하고, 다음 클릭부터 기존 이동 동작을 실행합니다.</summary>
    public bool CompleteTyping()
    {
        if (!isTyping || messageText == null) return false;
        StopTyping();
        messageText.text = fullMessage;
        RefreshLayout(true);
        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
            CompleteTyping();
    }

    private void StopTyping()
    {
        isTyping = false;
        revealEnds.Clear();
        scrollToLatest = false;
        layoutVelocity = Vector2.zero;
    }

    private Vector2 AvailableSize()
    {
        var parent = transform.parent as RectTransform;
        return parent != null && parent.rect.width > 0f
            ? parent.rect.size : new Vector2(800f, 600f);
    }

    private void RefreshLayout(bool resetScroll)
    {
        if (msgBox == null || messageText == null || messageViewport == null) return;

        var root = (RectTransform)transform;
        lastAvailableSize = AvailableSize();
        laidOutText = messageText.text;

        // Keep the portrait and tail fixed; only the sliced body stretches.
        float availableWidth = lastAvailableSize.x - Mathf.Max(screenMargin, root.anchoredPosition.x)
            - bubbleOffset.x - navigationOverhang - screenMargin;
        float widthLimit = Mathf.Max(1f, Mathf.Min(maximumWidth, availableWidth));
        float naturalWidth = messageText.GetPreferredValues(laidOutText, Mathf.Infinity, Mathf.Infinity).x;
        float width = Mathf.Clamp(naturalWidth + textPadding.x + textPadding.z,
            Mathf.Min(minimumWidth, widthLimit), widthLimit);
        float textWidth = Mathf.Max(1f, width - textPadding.x - textPadding.z);
        float textHeight = messageText.GetPreferredValues(laidOutText, textWidth, Mathf.Infinity).y;
        float heightLimit = Mathf.Max(minimumHeight,
            lastAvailableSize.y * maximumScreenHeight - bubbleOffset.y - bottomMargin);
        float height = Mathf.Clamp(textHeight + textPadding.y + textPadding.w, minimumHeight, heightLimit);
        targetBodySize = new Vector2(width, height);
        targetTextWidth = textWidth;
        targetTextHeight = textHeight;
        resetScrollPending |= resetScroll;
        layoutNeedsApply = true;
        if (!hasLayout || !Application.isPlaying || resizeSmoothTime <= 0f)
        {
            hasLayout = true;
            layoutVelocity = Vector2.zero;
            ApplyLayout(targetBodySize);
        }
    }

    private void ApplyLayout(Vector2 size)
    {
        var root = (RectTransform)transform;
        float viewportHeight = Mathf.Max(1f, size.y - textPadding.y - textPadding.w);

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleOffset.x + size.x + navigationOverhang);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bubbleOffset.y + size.y + bottomMargin);
        msgBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        msgBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        messageViewport.anchoredPosition = new Vector2(textPadding.x, -textPadding.y);
        // The right padding also buffers the short resize lag, so a newly typed glyph is
        // not cut in half while the bubble catches up. Content still keeps its full padding.
        float rightClipInset = Mathf.Min(3f, textPadding.z);
        messageViewport.sizeDelta = new Vector2(Mathf.Max(1f, size.x - textPadding.x - rightClipInset), viewportHeight);
        // Lay out the text at its destination width, while the viewport follows the background.
        // Otherwise the growing background makes the last word wrap back and forth every frame.
        messageText.rectTransform.sizeDelta = new Vector2(targetTextWidth, Mathf.Max(targetTextHeight, viewportHeight));

        if (messageScroll != null)
        {
            messageScroll.vertical = targetTextHeight > targetBodySize.y - textPadding.y - textPadding.w + 0.5f;
            if (resetScrollPending || !messageScroll.vertical)
            {
                messageScroll.StopMovement();
                messageText.rectTransform.anchoredPosition = Vector2.zero;
                messageScroll.verticalNormalizedPosition = 1f;
            }
            else if (scrollToLatest)
            {
                messageScroll.StopMovement();
                messageScroll.verticalNormalizedPosition = 0f;
            }
        }
        resetScrollPending = false;
        scrollToLatest = false;
        layoutNeedsApply = false;
    }

    private void CacheShownTransform()
    {
        if (msgBox == null || hasShownTransform) return;
        shownPosition = msgBox.anchoredPosition;
        shownPivot = msgBox.pivot;
        shownScale = msgBox.localScale;
        hasShownTransform = true;
    }

    private void PlayShowAnimation()
    {
        if (msgBox == null) return;
        CacheShownTransform();
        showTween?.Kill();

        Vector2 size = msgBox.rect.size;
        msgBox.pivot = new Vector2(0f, shownPivot.y);
        msgBox.anchoredPosition = shownPosition + new Vector2(-shownPivot.x * size.x, 0f);
        msgBox.localScale = new Vector3(shownScale.x * collapsedScaleX, shownScale.y, shownScale.z);

        showTween = msgBox
            .DOScaleX(shownScale.x, showDuration)
            .SetEase(showEase)
            .SetUpdate(true)
            .SetLink(msgBox.gameObject)
            .OnComplete(RestoreShownTransform);
    }

    private void StopShowAnimation()
    {
        showTween?.Kill();
        showTween = null;
        RestoreShownTransform();
    }

    private void RestoreShownTransform()
    {
        if (msgBox == null || !hasShownTransform) return;
        msgBox.pivot = shownPivot;
        msgBox.anchoredPosition = shownPosition;
        msgBox.localScale = shownScale;
    }

    private void HandlePrevious()
    {
        if (!CompleteTyping()) onPrevious?.Invoke();
    }

    private void HandleNext()
    {
        if (!CompleteTyping()) onNext?.Invoke();
    }

    private void HandleConfirm()
    {
        if (!CompleteTyping()) onConfirm?.Invoke();
    }
}
