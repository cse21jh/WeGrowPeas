using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Coordinates the small phone button and the full phone without changing app state.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/Phone/Phone Panel Motion")]
public sealed class PhonePanelMotion : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private RectTransform phoneRoot;
    [SerializeField] private RectTransform toggleRoot;
    [SerializeField] private RectTransform expandIcon;
    [SerializeField] private CanvasGroup phoneGroup;
    [SerializeField] private CanvasGroup toggleGroup;

    [Header("Hover / 뽈롱 확대")]
    [SerializeField, Min(1f)] private float buttonHoverScale = 1.08f;
    [Tooltip("부모 버튼 확대에 추가로 적용하는 아이콘 배율입니다.")]
    [SerializeField, Min(1f)] private float iconHoverScale = 1.22f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.2f;
    [SerializeField, Min(0f)] private float popOvershoot = 1.6f;

    [Header("Open and Close / 열기와 닫기")]
    [SerializeField, Min(0.01f)] private float buttonHideDuration = 0.13f;
    [SerializeField, Min(0.01f)] private float buttonShowDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float phoneSlideDuration = 0.32f;
    [Tooltip("화면 우측 바깥으로 이동한 뒤 추가로 확보할 여백(Canvas 단위).")]
    [SerializeField, Min(0f)] private float offscreenPadding = 24f;

    private Vector2 phoneRestPosition;
    private Vector3 buttonRestScale;
    private Vector3 iconRestScale;
    private bool initialized;
    private bool targetOpen;
    private bool phoneInteractable, phoneBlocksRaycasts;
    private bool toggleInteractable, toggleBlocksRaycasts;
    private Sequence transition;
    private Sequence hover;
    private readonly Vector3[] corners = new Vector3[4];

    public bool IsTransitioning => transition != null && transition.IsActive();

    private bool Initialize()
    {
        if (initialized) return true;
        if (phoneRoot == null || toggleRoot == null) return false;
        phoneRestPosition = phoneRoot.anchoredPosition;
        buttonRestScale = toggleRoot.localScale;
        if (expandIcon != null) iconRestScale = expandIcon.localScale;
        if (phoneGroup != null)
        {
            phoneInteractable = phoneGroup.interactable;
            phoneBlocksRaycasts = phoneGroup.blocksRaycasts;
        }
        if (toggleGroup != null)
        {
            toggleInteractable = toggleGroup.interactable;
            toggleBlocksRaycasts = toggleGroup.blocksRaycasts;
        }
        initialized = true;
        return true;
    }

    public void SetHovered(bool value)
    {
        if (!Initialize() || targetOpen || IsTransitioning) return;
        var button = toggleRoot.GetComponent<Button>();
        if (value && (button == null || !button.IsInteractable())) return;
        KillHover();
        hover = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        hover.Join(toggleRoot.DOScale(buttonRestScale * (value ? buttonHoverScale : 1f), hoverDuration)
            .SetEase(value ? Ease.OutBack : Ease.OutQuad, popOvershoot));
        if (expandIcon != null)
            hover.Join(expandIcon.DOScale(iconRestScale * (value ? iconHoverScale : 1f), hoverDuration)
                .SetEase(value ? Ease.OutBack : Ease.OutQuad, popOvershoot));
    }

    public void ClearHover()
    {
        if (!initialized || targetOpen || IsTransitioning) return;
        KillHover();
        toggleRoot.localScale = buttonRestScale;
        if (expandIcon != null) expandIcon.localScale = iconRestScale;
    }

    public void SetOpenImmediate(bool open)
    {
        if (!Initialize()) return;
        KillTransition();
        KillHover();
        targetOpen = open;
        ApplySettledState();
    }

    public void SetOpen(bool open, Action completed = null)
    {
        if (!Initialize()) { completed?.Invoke(); return; }
        if (!isActiveAndEnabled)
        {
            SetOpenImmediate(open);
            completed?.Invoke();
            return;
        }

        // Retarget from the current pose, including a partially completed hover
        // or opposite transition. Never complete a cancelled sequence's callbacks.
        KillTransition();
        KillHover();
        targetOpen = open;
        SetInput(false, false);
        Vector2 hiddenPosition = GetHiddenPhonePosition();
        if (open && !phoneRoot.gameObject.activeSelf)
        {
            phoneRoot.anchoredPosition = hiddenPosition;
            // Activate before PhoneManager resumes a Messenger coroutine, but
            // keep the panel offscreen until the small button has disappeared.
            phoneRoot.gameObject.SetActive(true);
        }

        transition = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        if (open)
        {
            float shrink = buttonHideDuration * Mathf.Clamp01(toggleRoot.localScale.magnitude / Mathf.Max(buttonRestScale.magnitude, 0.001f));
            transition.Append(toggleRoot.DOScale(Vector3.zero, Mathf.Max(shrink, 0.001f)).SetEase(Ease.InCubic));
            transition.AppendCallback(() => toggleRoot.gameObject.SetActive(false));
            transition.Append(phoneRoot.DOAnchorPos(phoneRestPosition, SlideDuration(phoneRestPosition, hiddenPosition)).SetEase(Ease.OutCubic));
        }
        else
        {
            transition.Append(phoneRoot.DOAnchorPos(hiddenPosition, SlideDuration(hiddenPosition, hiddenPosition)).SetEase(Ease.InCubic));
            transition.AppendCallback(() =>
            {
                phoneRoot.gameObject.SetActive(false);
                toggleRoot.gameObject.SetActive(true);
            });
            transition.Append(toggleRoot.DOScale(buttonRestScale, buttonShowDuration).SetEase(Ease.OutBack, popOvershoot));
            if (expandIcon != null)
                transition.Join(expandIcon.DOScale(iconRestScale, buttonShowDuration).SetEase(Ease.OutQuad));
        }

        transition.OnComplete(() =>
        {
            transition = null;
            ApplySettledState();
            completed?.Invoke();
        });
    }

    private float SlideDuration(Vector2 destination, Vector2 hidden)
    {
        float fullDistance = Vector2.Distance(phoneRestPosition, hidden);
        float remaining = Vector2.Distance(phoneRoot.anchoredPosition, destination);
        return Mathf.Max(0.001f, phoneSlideDuration * Mathf.Clamp01(remaining / Mathf.Max(fullDistance, 0.001f)));
    }

    private Vector2 GetHiddenPhonePosition()
    {
        var parent = phoneRoot.parent as RectTransform;
        var canvas = phoneRoot.GetComponentInParent<Canvas>(true);
        var viewport = canvas != null ? canvas.rootCanvas.transform as RectTransform : parent;
        if (parent == null || viewport == null)
            return phoneRestPosition + Vector2.right * (phoneRoot.rect.width + offscreenPadding);

        phoneRoot.GetWorldCorners(corners);
        float left = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
            left = Mathf.Min(left, viewport.InverseTransformPoint(corners[i]).x);
        float distance = viewport.rect.xMax + offscreenPadding - left;
        Vector3 offset = parent.InverseTransformVector(viewport.TransformVector(Vector3.right * distance));
        return phoneRoot.anchoredPosition + new Vector2(offset.x, offset.y);
    }

    private void ApplySettledState()
    {
        phoneRoot.anchoredPosition = phoneRestPosition;
        toggleRoot.localScale = targetOpen ? Vector3.zero : buttonRestScale;
        if (expandIcon != null) expandIcon.localScale = iconRestScale;
        phoneRoot.gameObject.SetActive(targetOpen);
        toggleRoot.gameObject.SetActive(!targetOpen);
        SetInput(targetOpen, !targetOpen);
    }

    private void SetInput(bool phone, bool toggle)
    {
        if (phoneGroup != null)
        {
            phoneGroup.interactable = phone && phoneInteractable;
            phoneGroup.blocksRaycasts = phone && phoneBlocksRaycasts;
        }
        if (toggleGroup != null)
        {
            toggleGroup.interactable = toggle && toggleInteractable;
            toggleGroup.blocksRaycasts = toggle && toggleBlocksRaycasts;
        }
    }

    private void KillHover() { hover?.Kill(); hover = null; }
    private void KillTransition() { transition?.Kill(); transition = null; }

    private void OnDisable()
    {
        KillTransition();
        KillHover();
        if (initialized) ApplySettledState();
    }

    private void OnDestroy()
    {
        KillTransition();
        KillHover();
    }
}
