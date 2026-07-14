using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class HoverTooltipUI : BasePopup, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icon Elements")]
    [SerializeField] private Image iconImage;

    [Header("Tooltip Panel")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float startLocalX = 0f;     // 말풍선이 튀어나오기 시작할 안쪽 X 좌표
    [SerializeField] private float animDuration = 0.25f; // 애니메이션 재생 시간
    [SerializeField] private Ease showEase = Ease.OutBack; // 튀어나올 때 탄성 효과
    [SerializeField] private Ease hideEase = Ease.InQuad;  // 들어갈 때 가속 효과

    private float targetLocalX;
    private Tween fadeTween;
    private Tween scaleTween;
    private Tween moveTween;

    protected override void Awake()
    {
        base.Awake();
        if (tooltipPanel != null)
        {
            targetLocalX = tooltipPanel.localPosition.x;
        }
        ResetTooltipState();
    }

    public void Setup(Sprite iconSprite, string description, System.Action onClose = null)
    {
        onCloseCallback = onClose;

        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = iconSprite;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        ResetTooltipState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel == null || tooltipCanvasGroup == null) return;

        tooltipPanel.gameObject.SetActive(true);

        fadeTween?.Kill();
        scaleTween?.Kill();
        moveTween?.Kill();

        Vector3 pos = tooltipPanel.localPosition;
        pos.x = startLocalX;
        tooltipPanel.localPosition = pos;

        fadeTween = tooltipCanvasGroup.DOFade(1f, animDuration).SetUpdate(true);
        scaleTween = tooltipPanel.DOScale(Vector3.one, animDuration).SetEase(showEase).SetUpdate(true);
        moveTween = tooltipPanel.DOLocalMoveX(targetLocalX, animDuration).SetEase(showEase).SetUpdate(true);
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
        moveTween?.Kill();

        float duration = animDuration * 0.8f;

        fadeTween = tooltipCanvasGroup.DOFade(0f, duration).SetUpdate(true);
        scaleTween = tooltipPanel.DOScale(new Vector3(0f, 1f, 1f), duration).SetEase(hideEase).SetUpdate(true);
        moveTween = tooltipPanel.DOLocalMoveX(startLocalX, duration).SetEase(hideEase).SetUpdate(true)
            .OnComplete(() => tooltipPanel.gameObject.SetActive(false));
    }

    private void ResetTooltipState()
    {
        fadeTween?.Kill();
        scaleTween?.Kill();
        moveTween?.Kill();

        if (tooltipCanvasGroup != null)
        {
            tooltipCanvasGroup.alpha = 0f;
        }

        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
            tooltipPanel.localScale = new Vector3(0f, 1f, 1f);

            Vector3 pos = tooltipPanel.localPosition;
            pos.x = startLocalX;
            tooltipPanel.localPosition = pos;
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
