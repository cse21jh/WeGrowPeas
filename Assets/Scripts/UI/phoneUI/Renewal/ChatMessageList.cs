using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메시지의 스테이지에 따라 Initial 또는 Normal 프리팹을 생성한다.
/// </summary>
public sealed class ChatMessageList : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private RectTransform content;

    [Header("Message Prefabs")]
    [SerializeField]
    private ChatMessageItem initialMessagePrefab;

    [SerializeField]
    private ChatMessageItem normalMessagePrefab;

    [Header("Sender")]
    [SerializeField]
    private string senderName = "정부";

    [SerializeField]
    private Sprite senderProfileSprite;

    [Header("Scroll")]
    [SerializeField]
    private bool scrollToBottomOnAdd = true;

    private bool hasPreviousMessage;
    private int previousStageId;

    private Coroutine layoutRefreshCoroutine;

    [SerializeField] private ChatMessageData[] initialMessages;


    private void Start()
    {
        foreach (var messageData in initialMessages)
        {
            AddMessage(messageData.StageId, messageData.Message);
        }
    }

    /// <summary>
    /// 메시지를 추가한다.
    /// 해당 스테이지의 첫 메시지는 Initial 프리팹을 사용한다.
    /// </summary>
    public void AddMessage(int stageId, string message)
    {
        bool isFirstMessageOfStage =
            !hasPreviousMessage ||
            previousStageId != stageId;

        ChatMessageItem selectedPrefab =
            isFirstMessageOfStage
                ? initialMessagePrefab
                : normalMessagePrefab;

        ChatMessageItem item = Instantiate(
            selectedPrefab,
            content,
            false);

        item.Setup(
            message,
            senderName,
            senderProfileSprite);

        previousStageId = stageId;
        hasPreviousMessage = true;

        RequestLayoutRefresh();
    }

    /// <summary>
    /// 발신자 정보를 런타임에 변경한다.
    /// 이후 추가되는 Initial 메시지부터 적용된다.
    /// </summary>
    public void SetSender(
        string newSenderName,
        Sprite newProfileSprite)
    {
        senderName = newSenderName;
        senderProfileSprite = newProfileSprite;
    }

    public void ClearMessages()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        hasPreviousMessage = false;
        previousStageId = 0;

        RequestLayoutRefresh();
    }

    private void RequestLayoutRefresh()
    {
        if (layoutRefreshCoroutine != null)
        {
            StopCoroutine(layoutRefreshCoroutine);
        }

        layoutRefreshCoroutine =
            StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        if (scrollToBottomOnAdd && scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        layoutRefreshCoroutine = null;
    }
}
