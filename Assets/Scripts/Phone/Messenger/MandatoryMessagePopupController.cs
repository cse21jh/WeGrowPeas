using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MandatoryMessagePopupController : MonoBehaviour
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

    private Action onPrevious;
    private Action onNext;
    private Action onConfirm;
    private Tween showTween;
    private Vector2 shownPosition;
    private Vector2 shownPivot;
    private Vector3 shownScale;
    private bool hasShownTransform;

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

    public void Show(ChatPartner partner, string body)
    {
        bool wasOpen = gameObject.activeSelf;
        gameObject.SetActive(true);

        if (!wasOpen)
            PlayShowAnimation();

        if (profileImage != null)
        {
            if (partner != null && partner.chatPartnerImage != null)
                profileImage.sprite = partner.chatPartnerImage;
            profileImage.enabled = profileImage.sprite != null;
        }

        if (partnerNameText != null)
            partnerNameText.text = partner != null ? partner.chatPartnerName : string.Empty;

        if (messageText != null)
            messageText.text = body ?? string.Empty;
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
        StopShowAnimation();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopShowAnimation();
    }

    private void OnDestroy()
    {
        showTween?.Kill();
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

    private void HandlePrevious() => onPrevious?.Invoke();
    private void HandleNext() => onNext?.Invoke();
    private void HandleConfirm() => onConfirm?.Invoke();
}
