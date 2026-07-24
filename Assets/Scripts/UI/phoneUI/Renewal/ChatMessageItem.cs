using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메시지 텍스트에 맞춰 말풍선 크기와
/// 메시지 항목 전체 높이를 조절한다.
/// 
/// Initial 메시지인 경우에만 프로필 이미지와 발신자 이름을 설정한다.
/// </summary>
[RequireComponent(typeof(LayoutElement))]
public sealed class ChatMessageItem : MonoBehaviour
{
    [Header("Message Type")]
    [Tooltip("날짜/스테이지의 최초 메시지 프리팹인 경우 체크")]
    [SerializeField]
    private bool isInitMsg;

    [Header("Common References")]
    [SerializeField]
    private RectTransform bubbleRect;

    [SerializeField]
    private TextMeshProUGUI messageText;

    [SerializeField]
    private LayoutElement layoutElement;

    [Header("Initial Message References")]
    [Tooltip("Is Init Msg가 true인 프리팹에서만 연결")]
    [SerializeField]
    private Image profileImage;

    [Tooltip("Is Init Msg가 true인 프리팹에서만 연결")]
    [SerializeField]
    private TextMeshProUGUI senderNameText;

    [Header("Bubble Size")]
    [Tooltip("말풍선이 늘어날 수 있는 최대 너비")]
    [SerializeField]
    private float maximumBubbleWidth = 140f;

    [Header("Item Size")]
    [Tooltip("프리팹의 최소 높이")]
    [SerializeField]
    private float minimumItemHeight = 25f;

    [Tooltip("말풍선 아래쪽에 남길 여백")]
    [SerializeField]
    private float bottomPadding = 3f;

    private RectTransform messageRect;

    private float minimumBubbleWidth;
    private float minimumBubbleHeight;

    private void Awake()
    {
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        messageRect = messageText.rectTransform;

        // 프리팹에 직접 설정한 말풍선 크기를 최소 크기로 사용한다.
        minimumBubbleWidth = bubbleRect.rect.width;
        minimumBubbleHeight = bubbleRect.rect.height;
    }

    /// <summary>
    /// 메시지 내용을 설정한다.
    /// Initial 메시지라면 발신자 이름과 프로필 이미지도 설정한다.
    /// </summary>
    public void Setup(
        string message,
        string senderName,
        Sprite profileSprite)
    {
        messageText.text = message ?? string.Empty;

        if (isInitMsg)
        {
            ApplySenderInfo(senderName, profileSprite);
        }

        ResizeBubble();
        UpdateItemHeight();
    }

    /// <summary>
    /// Initial 메시지에만 발신자 정보를 적용한다.
    /// </summary>
    private void ApplySenderInfo(
        string senderName,
        Sprite profileSprite)
    {
        if (senderNameText != null)
        {
            senderNameText.text = senderName ?? string.Empty;
        }

        if (profileImage != null)
        {
            profileImage.sprite = profileSprite;
            profileImage.enabled = profileSprite != null;
        }
    }

    /// <summary>
    /// 텍스트 양에 맞춰 말풍선 크기를 조정한다.
    /// </summary>
    private void ResizeBubble()
    {
        float horizontalPadding = GetHorizontalPadding();
        float verticalPadding = GetVerticalPadding();

        float maximumTextWidth = Mathf.Max(
            1f,
            maximumBubbleWidth - horizontalPadding);

        // 최대 너비 안에서 텍스트가 차지할 크기를 계산한다.
        Vector2 preferredSize = messageText.GetPreferredValues(
            messageText.text,
            maximumTextWidth,
            Mathf.Infinity);

        float bubbleWidth = Mathf.Clamp(
            Mathf.Ceil(preferredSize.x + horizontalPadding),
            minimumBubbleWidth,
            maximumBubbleWidth);

        // 최종 말풍선 너비를 기준으로 높이를 다시 계산한다.
        float finalTextWidth = Mathf.Max(
            1f,
            bubbleWidth - horizontalPadding);

        Vector2 finalPreferredSize = messageText.GetPreferredValues(
            messageText.text,
            finalTextWidth,
            Mathf.Infinity);

        float bubbleHeight = Mathf.Max(
            minimumBubbleHeight,
            Mathf.Ceil(finalPreferredSize.y + verticalPadding));

        // 프리팹에 설정된 위치는 유지하고 크기만 변경한다.
        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            bubbleWidth);

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            bubbleHeight);
    }

    /// <summary>
    /// VerticalLayoutGroup이 사용할 메시지 항목 높이를 갱신한다.
    /// </summary>
    private void UpdateItemHeight()
    {
        /*
         * Bubble의 Pivot과 위치를 고려해
         * 프리팹 루트 상단에서 Bubble 하단까지의 거리를 계산한다.
         */
        float bubbleBottom =
            -bubbleRect.anchoredPosition.y +
            bubbleRect.rect.height * bubbleRect.pivot.y;

        float itemHeight = Mathf.Max(
            minimumItemHeight,
            bubbleBottom + bottomPadding);

        layoutElement.preferredHeight = itemHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private float GetHorizontalPadding()
    {
        return messageRect.offsetMin.x - messageRect.offsetMax.x;
    }

    private float GetVerticalPadding()
    {
        return messageRect.offsetMin.y - messageRect.offsetMax.y;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        maximumBubbleWidth = Mathf.Max(1f, maximumBubbleWidth);
        minimumItemHeight = Mathf.Max(1f, minimumItemHeight);
        bottomPadding = Mathf.Max(0f, bottomPadding);
    }
#endif
}
