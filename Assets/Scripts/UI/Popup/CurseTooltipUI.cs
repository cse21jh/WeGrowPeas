using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CurseTooltipUI : BasePopup, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icon Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI durationText;

    [Header("Tooltip Panel")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.25f; // 애니메이션 재생 시간
    [SerializeField] private Ease showEase = Ease.OutBack; // 튀어나올 때 탄성 효과
    [SerializeField] private Ease hideEase = Ease.InQuad;  // 들어갈 때 가속 효과

    private Tween fadeTween;
    private Tween scaleTween;

    protected override void Awake()
    {
        base.Awake();
        if (tooltipPanel != null)
        {
            SetPivotLeft(tooltipPanel);
        }
        ResetTooltipState();
    }

    private void SetPivotLeft(RectTransform rect)
    {
        if (rect == null) return;
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = rect.pivot - new Vector2(0f, rect.pivot.y);
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x * rect.localScale.x, deltaPivot.y * size.y * rect.localScale.y);
        rect.pivot = new Vector2(0f, rect.pivot.y);
        rect.localPosition -= deltaPosition;
    }

    public void Setup(Sprite iconSprite, string description, int daysLeft = -1, System.Action onClose = null)
    {
        onCloseCallback = onClose;

        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = iconSprite;
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (durationText != null)
        {
            if (daysLeft > 0)
            {
                durationText.gameObject.SetActive(true);
                durationText.text = $"{daysLeft}일";
            }
            else
            {
                durationText.gameObject.SetActive(false);
            }
        }

        ResetTooltipState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel == null || tooltipCanvasGroup == null) return;

        tooltipPanel.gameObject.SetActive(true);

        fadeTween?.Kill();
        scaleTween?.Kill();

        fadeTween = tooltipCanvasGroup.DOFade(1f, animDuration).SetUpdate(true);
        scaleTween = tooltipPanel.DOScale(Vector3.one, animDuration).SetEase(showEase).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltipPanel();
    }

    private void HideTooltipPanel()
    {
        if (tooltipPanel == null || tooltipCanvasGroup == null) return;

        fadeTween?.Kill();
        scaleTween?.Kill();

        float duration = animDuration * 0.8f;

        fadeTween = tooltipCanvasGroup.DOFade(0f, duration).SetUpdate(true);
        scaleTween = tooltipPanel.DOScale(new Vector3(0f, 1f, 1f), duration).SetEase(hideEase).SetUpdate(true)
            .OnComplete(() => tooltipPanel.gameObject.SetActive(false));
    }

    private void ResetTooltipState()
    {
        fadeTween?.Kill();
        scaleTween?.Kill();

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
            tooltipPanel.localScale = new Vector3(0f, 1f, 1f);
        }
    }

    private void OnDisable()
    {
        ResetTooltipState();
    }

    public override void Close()
    {
        ResetTooltipState();
        base.Close();
    }
}
