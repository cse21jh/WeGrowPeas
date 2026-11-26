using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(9000)]
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
    [Tooltip("조달청 대화 효과와 동일하게 TMP 글자를 순서대로 표시합니다.")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 36f;
    [SerializeField, Min(0f)] private float typingStartDelay = 0.08f;
    [Tooltip("표시된 글자에 맞춰 말풍선 크기가 따라가는 시간. 0이면 즉시 조절합니다.")]
    [SerializeField, Min(0f)] private float resizeSmoothTime = 0.08f;

    [Header("Fixed Width Bubble Layout")]
    [SerializeField] private ScrollRect messageScroll;
    [SerializeField] private RectTransform messageViewport;
    [Tooltip("텍스트 양과 무관한 말풍선 가로 폭입니다. 화면보다 넓을 때만 줄어듭니다.")]
    [FormerlySerializedAs("maximumWidth")]
    [SerializeField, Min(1f)] private float fixedWidth = 420f;
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
    private string fullMessage = string.Empty;
    private int revealedElements;
    private int notifiedCharacters;
    private int laidOutCharacters;
    private Tween revealTween;
    private SpeechTextBounce textBounce;
    private bool isTyping;
    private bool hasLayout;
    private bool layoutNeedsApply;
    private bool resetScrollPending;
    private bool scrollToLatest;
    private Vector2 targetBodySize;
    private Vector2 layoutVelocity;
    private float targetTextWidth;
    private float targetTextHeight;
    private float fullTextHeight;

    public bool IsTyping => isTyping;
    public event Action<char, int> CharacterRevealed;
    public event Action RevealStopped;

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
        revealTween?.Kill();
    }

    private void LateUpdate()
    {
        if (messageText == null) return;
        bool revealed = UpdateTyping();
        Vector2 available = AvailableSize();
        if (available != lastAvailableSize || laidOutText != messageText.text ||
            laidOutCharacters != messageText.maxVisibleCharacters)
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

        // JODAL's reveal keeps the complete string in TMP, including rich text and wrapping.
        messageText.ForceMeshUpdate(true);
        int characterCount = messageText.textInfo.characterCount;
        if (characterCount == 0) return;
        messageText.maxVisibleCharacters = 0;
        revealedElements = 0;
        notifiedCharacters = 0;
        isTyping = true;
        if (textBounce == null) textBounce = messageText.GetComponent<SpeechTextBounce>();
        if (textBounce != null) textBounce.BeginReveal();
        var tween = messageText.DOMaxVisibleCharacters(characterCount,
                characterCount / Mathf.Max(1f, charactersPerSecond))
            .SetDelay(typingStartDelay)
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Late, true)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        revealTween = tween;
        tween.OnUpdate(NotifyRevealedCharacters);
        tween.OnComplete(() => isTyping = false);
        tween.OnKill(() => { if (revealTween == tween) { revealTween = null; isTyping = false; } });
    }

    private void NotifyRevealedCharacters()
    {
        if (!isTyping || messageText == null) return;
        var info = messageText.textInfo;
        int visible = Mathf.Min(messageText.maxVisibleCharacters, info.characterCount);
        while (notifiedCharacters < visible)
        {
            int index = notifiedCharacters++;
            if (info.characterInfo[index].elementType == TMP_TextElementType.Character)
                CharacterRevealed?.Invoke(info.characterInfo[index].character, index);
        }
    }

    private bool UpdateTyping()
    {
        int count = messageText.maxVisibleCharacters;
        if (count == revealedElements) return false;
        revealedElements = count;
        scrollToLatest = true;
        return true;
    }

    /// <summary>첫 클릭은 현재 문장을 완성하고, 다음 클릭부터 기존 이동 동작을 실행합니다.</summary>
    public bool CompleteTyping()
    {
        if (!isTyping || messageText == null) return false;
        StopTyping();
        messageText.text = fullMessage;
        messageText.maxVisibleCharacters = int.MaxValue;
        RefreshLayout(true);
        // Completing a message also reveals its full viewport in this frame.
        if (hasLayout) ApplyLayout(targetBodySize);
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
        RevealStopped?.Invoke();
        revealTween?.Kill();
        revealTween = null;
        if (textBounce != null) textBounce.FinishInstantly();
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
        laidOutCharacters = messageText.maxVisibleCharacters;
        string visibleText = VisibleLayoutText();

        // Width depends only on the available screen, never on the revealed text.
        float availableWidth = lastAvailableSize.x - Mathf.Max(screenMargin, root.anchoredPosition.x)
            - bubbleOffset.x - navigationOverhang - screenMargin;
        float width = Mathf.Max(1f, Mathf.Min(fixedWidth, availableWidth));
        float textWidth = Mathf.Max(1f, width - textPadding.x - textPadding.z);
        fullTextHeight = messageText.GetPreferredValues(laidOutText, textWidth, Mathf.Infinity).y;
        float textHeight = messageText.GetPreferredValues(visibleText, textWidth, Mathf.Infinity).y;
        float heightLimit = Mathf.Max(minimumHeight,
            lastAvailableSize.y * maximumScreenHeight - bubbleOffset.y - bottomMargin);
        float height = Mathf.Clamp(textHeight + textPadding.y + textPadding.w, minimumHeight, heightLimit);
        targetBodySize = new Vector2(width, height);
        targetTextWidth = textWidth;
        targetTextHeight = textHeight;
        resetScrollPending |= resetScroll;
        layoutNeedsApply = true;
        // A screen resize must not animate the width or reflow text at an intermediate width.
        if (hasLayout && !Mathf.Approximately(msgBox.rect.width, width))
        {
            layoutVelocity.x = 0f;
            ApplyLayout(new Vector2(width, msgBox.rect.height));
        }
        if (!hasLayout || !Application.isPlaying || resizeSmoothTime <= 0f)
        {
            hasLayout = true;
            layoutVelocity = Vector2.zero;
            ApplyLayout(targetBodySize);
        }
    }

    private string VisibleLayoutText()
    {
        int count = messageText.maxVisibleCharacters;
        var info = messageText.textInfo;
        if (count <= 0) return string.Empty;
        if (count >= info.characterCount) return messageText.text;
        var last = info.characterInfo[count - 1];
        int end = Mathf.Clamp(last.index + last.stringLength, 0, messageText.text.Length);
        return messageText.text.Substring(0, end);
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
        // Leave a little room for the existing glyph bounce at the clipping edge.
        float rightClipInset = Mathf.Min(3f, textPadding.z);
        messageViewport.sizeDelta = new Vector2(Mathf.Max(1f, size.x - textPadding.x - rightClipInset), viewportHeight);
        // Text and background share the same fixed wrapping width throughout the reveal.
        messageText.rectTransform.sizeDelta = new Vector2(targetTextWidth, Mathf.Max(fullTextHeight, viewportHeight));

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
                // The full string is laid out already; follow the revealed lines, not the hidden ending.
                messageScroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(
                    (targetTextHeight - viewportHeight) / Mathf.Max(0.001f, fullTextHeight - viewportHeight));
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
