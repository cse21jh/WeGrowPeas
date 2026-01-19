using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessengerProgress
{
    // Key: Chat의 이름, Value: 마지막으로 본 메시지의 인덱스
    public Dictionary<string, int> conversationSeenIndices = new Dictionary<string, int>();

    // 활성화된 모든 트리거 ID들을 저장 (중복 없이)
    public HashSet<string> activatedTriggers = new HashSet<string>();
}

public struct UnreadInfo
{
    public bool hasUnread;
    public bool hasMandatory;
}


public class MessengerApp : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<Chat> allChats;

    [Header("UI Panels")]
    [SerializeField] private GameObject chatPartnerListPanel;
    [SerializeField] private GameObject chatRoomPanel;

    [Header("UI Components")]
    [SerializeField] private Transform chatPartnerListContent;    
    [SerializeField] private Button backTochatPartnersButton;
    [SerializeField] private GameObject mandatoryMessageIndicator; // 필수 메시지 알림 (예: 앱 아이콘 위의 빨간 점)

    [SerializeField] private Transform chatRoomContent;    
    [SerializeField] private TMP_Text chatRoomHeaderName;
    [SerializeField] private Image chatRoomHeaderImage;
    [SerializeField] private MessageController mc;

    [SerializeField] private GameObject chatPartnerListItemPrefab;    

    private MessengerProgress progress;
    private Chat currentChat;
    private Coroutine messageDisplayCoroutine;
    
    public bool IsDisplayingMessages { get; private set; } = false;

    void Awake()
    {
        progress = new MessengerProgress();
        // TODO: Save/Load

        // --- 초기화 로직 추가 ---
        // 만약 로드된 데이터가 없다면 (새 게임이라면) 딕셔너리를 초기화
        if (progress.conversationSeenIndices.Count == 0)
        {
            InitializeConversationIndices();
        }
    }

    private void InitializeConversationIndices()
    {
        if (allChats == null) return;

        foreach (var chat in allChats)
        {
            if (chat != null && chat.chatPartner != null)
            {
                // 아직 딕셔너리에 없는 대화 상대만 추가
                if (!progress.conversationSeenIndices.ContainsKey(chat.chatPartner.chatPartnerName))
                {
                    // -1은 "아직 아무 메시지도 보지 않았다"는 의미
                    progress.conversationSeenIndices.Add(chat.chatPartner.chatPartnerName, -1);
                }
            }
        }
    }

    void Start()
    {
        if (backTochatPartnersButton != null)
            backTochatPartnersButton.onClick.AddListener(OpenchatPartnerList);
    }  

    public void UpdateMessenger()
    {
        if (chatRoomPanel.activeSelf && currentChat != null)
        {
            // 만약 채팅방이 열려있다면, 해당 채팅방의 메시지만 갱신합니다.
            DisplayChatMessages();
        }
        else
        {
            // 대화 상대 목록이 열려있거나, 둘 다 닫혀있는 경우
            OpenchatPartnerList();
        }

    }

    public void ActivateTrigger(string triggerId)
    {
        // 이 triggerId를 사용하는 메시지가 하나라도 있는지 먼저 확인
        if (!DoesAnyMessageUseTrigger(triggerId))
            return;

        bool hadUnreadMandatoryBefore = HasUnreadMandatoryMessages();

        if (progress.activatedTriggers.Add(triggerId))
        {
            Debug.Log($"Messenger Trigger Activated: {triggerId}");
            // TODO: 세이브
        }

        if (gameObject.activeInHierarchy)
        {
            UpdateMessenger();
        }

        ReportAlarmState();
    }

    // 해당 트리거 ID를 사용하는 메시지가 있으면 true, 없으면 false를 반환
    private bool DoesAnyMessageUseTrigger(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId)) return false;

        foreach (var chat in allChats)
        {
            if (chat != null && chat.messages != null)
            {                
                if (chat.messages.Any(message => message.triggerId == triggerId))
                {
                    return true;
                }
            }
        }
        return false;
    }


    public void OpenchatPartnerList()
    {
        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        IsDisplayingMessages = false;

        chatPartnerListPanel.SetActive(true);
        chatRoomPanel.SetActive(false);
        currentChat = null;
        RefreshchatPartnerList();
    }

    public void OpenChatRoom(Chat conversation)
    {
        currentChat= conversation;
        chatPartnerListPanel.SetActive(false);
        chatRoomPanel.SetActive(true);

        chatRoomHeaderName.text = conversation.chatPartner.chatPartnerName;
        chatRoomHeaderImage.sprite = conversation.chatPartner.chatPartnerImage;

        DisplayChatMessages();
    }



    private void RefreshchatPartnerList()
    {
        foreach (Transform child in chatPartnerListContent) Destroy(child.gameObject);

        foreach (var chat in allChats)
        {
            if (chat == null || chat.messages == null || !HasAnyArrivedMessages(chat)) continue;

            // 안 읽은 메시지 정보 가져오기
            UnreadInfo unreadInfo = GetUnreadInfo(chat);

            // 미리보기 메시지 가져오기
            string previewMessage = GetPreviewMessageText(chat, unreadInfo.hasUnread);

            GameObject itemGO = Instantiate(chatPartnerListItemPrefab, chatPartnerListContent);
            ChatPartnerUI uiComponent = itemGO.GetComponent<ChatPartnerUI>();
            if (uiComponent != null)
            {
                uiComponent.Setup(chat, previewMessage, unreadInfo, this);
            }
            else
            {
                Debug.LogError($"Prefab '{chatPartnerListItemPrefab.name}' is missing ChatPartnerUI component.");
            }
        }
    }

    private UnreadInfo GetUnreadInfo(Chat chat)
    {
        UnreadInfo info = new UnreadInfo { hasUnread = false, hasMandatory = false };
        if (chat == null || chat.messages == null) return info;

        int lastSeenIndex = GetLastSeenIndex(chat);

        // 안 읽은 메시지들만 필터링
        var unreadMessages = chat.messages
            .Skip(lastSeenIndex + 1)
            .Where(msg => progress.activatedTriggers.Contains(msg.triggerId));

        if (unreadMessages.Any())
        {
            info.hasUnread = true;
            // 안 읽은 메시지 중에 '필수' 메시지가 있는지 확인
            if (unreadMessages.Any(msg => msg.isMandatory))
            {
                info.hasMandatory = true;
            }
        }
        return info;
    }

    private string GetPreviewMessageText(Chat chat, bool hasUnread)
    {
        if (chat == null || chat.messages == null || chat.messages.Count == 0) return "새로운 대화";

        if (hasUnread)
        {
            // 안 읽은 메시지가 있는 경우: 다음에 와야 할 첫 번째 메시지를 찾아서 반환
            int lastSeenIndex = GetLastSeenIndex(chat);
            var nextMessage = chat.messages
                .Skip(lastSeenIndex + 1)
                .FirstOrDefault(msg => progress.activatedTriggers.Contains(msg.triggerId));

            if (nextMessage != null)
            {
                return nextMessage.messageText;
            }
        }

        // 안 읽은 메시지가 없는 경우: 마지막으로 본 메시지를 반환
        int finalSeenIndex = GetLastSeenIndex(chat);
        if (finalSeenIndex >= 0 && finalSeenIndex < chat.messages.Count)
        {
            return chat.messages[finalSeenIndex].messageText;
        }

        // 아무 메시지도 본 적 없고, 안 읽은 메시지도 없는 이상한 경우
        // (또는 도착한 메시지가 아예 없는 경우)
        return "새로운 대화";
    }

    private void DisplayChatMessages()
    {     
        foreach (Transform child in chatRoomContent) Destroy(child.gameObject);

        if (currentChat == null) return;

        int lastSeenIndex = GetLastSeenIndex(currentChat);

        for (int i = 0; i <= lastSeenIndex; i++)
        {
            if (i < currentChat.messages.Count)
                CreateMessageBubble(currentChat.messages[i].messageText);
        }

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        messageDisplayCoroutine = StartCoroutine(ShowUnreadMessagesCoroutine());
    }

    private IEnumerator ShowUnreadMessagesCoroutine()
    {
        IsDisplayingMessages = true;
        bool hadUnreadMandatoryBefore = HasUnreadMandatoryMessages();

        bool isFirstUnreadMessage = true;

        if (currentChat == null)
        {
            IsDisplayingMessages = false;
            yield break;
        }

        int lastSeenIndex = GetLastSeenIndex(currentChat);

        for (int i = lastSeenIndex + 1; i < currentChat.messages.Count; i++)
        {
            ChatMessage message = currentChat.messages[i];
            if (progress.activatedTriggers.Contains(message.triggerId))
            {
                if (isFirstUnreadMessage)
                {
                    // 첫 번째 안 읽은 메시지라면, 딜레이 없이 바로 
                    isFirstUnreadMessage = false; 
                }
                else
                {
                    // 두 번째 이후의 안 읽은 메시지부터는 정상적으로 딜레이 적용
                    yield return new WaitForSeconds(message.delayAfterPrevious);
                }

                // 코루틴 실행 중 상태 변경에 대한 방어 
                if (currentChat == null || i >= currentChat.messages.Count || currentChat.messages[i].triggerId != message.triggerId)
                {
                    IsDisplayingMessages = false;
                    yield break;
                }

                CreateMessageBubble(message.messageText);
                SetLastSeenIndex(currentChat, i);
                ReportAlarmState();

                yield return null;
            }
            else
            {
                break;
            }
        }
        IsDisplayingMessages = false;

        bool hasUnreadMandatoryAfter = HasUnreadMandatoryMessages();

        if (hadUnreadMandatoryBefore && !hasUnreadMandatoryAfter)
        {
            // GameManager에 게임 재개를 요청
        }
    }

    private void CreateMessageBubble(string text)
    {
        mc.AddMessage(MessageController.MessageSenderType.pea, text);
    }    

    
    private bool HasAnyArrivedMessages(Chat chat) => chat.messages.Any(msg => progress.activatedTriggers.Contains(msg.triggerId));
    private bool HasUnreadMessages(Chat chat)
    {
        int lastSeenIndex = GetLastSeenIndex(chat);
        return chat.messages
            .Skip(lastSeenIndex + 1)
            .Any(msg => progress.activatedTriggers.Contains(msg.triggerId));
    }
    public void ReportAlarmState()
    {
        AlarmState currentState = AlarmState.None;

        if (HasUnreadMandatoryMessages())
        {
            currentState = AlarmState.Mandatory;
        }
        else if (HasUnreadMessagesForAllChats()) // 모든 채팅방 중 하나라도 안 읽은 게 있다면
        {
            currentState = AlarmState.NonMandatory;
        }
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Messenger, currentState);
    }

    private bool HasUnreadMessagesForAllChats()
    {
        return allChats.Any(chat => HasUnreadMessages(chat));
    }

    private bool HasUnreadMandatoryMessages()
    {
        foreach (var chat in allChats)
        {
            int lastSeenIndex = GetLastSeenIndex(chat);
            if (chat.messages.Skip(lastSeenIndex + 1)
                .Any(msg => msg.isMandatory && progress.activatedTriggers.Contains(msg.triggerId)))
                return true;
        }
        return false;
    }

    private string GetLastMessageText(Chat chat)
    {
        int lastSeenIndex = GetLastSeenIndex(chat);
        if (lastSeenIndex >= 0)
            return chat.messages[lastSeenIndex].messageText;

        var firstMessage = chat.messages.FirstOrDefault(msg => progress.activatedTriggers.Contains(msg.triggerId));
        return firstMessage != null ? firstMessage.messageText : "새로운 대화";
    }

    private int GetLastSeenIndex(Chat chat)
    {
        if (chat == null || chat.chatPartner == null) return -1;

        if (progress.conversationSeenIndices.TryGetValue(chat.chatPartner.chatPartnerName, out int index))
        {
            return index;
        }
        return -1; // 키가 없으면 "본 적 없음"을 의미하는 -1 반환
    }

    private void SetLastSeenIndex(Chat chat, int index)
    {
        progress.conversationSeenIndices[chat.chatPartner.chatPartnerName] = index;
        // TODO: SaveManager.Save(progress);
    }    
}
