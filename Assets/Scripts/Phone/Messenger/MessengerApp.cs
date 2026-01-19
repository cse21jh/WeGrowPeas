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
    [SerializeField] private ScrollRect scrollRect;


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
            // 핵심 변경: 화면을 다 지우지 않고, 현재 진행 중인 코루틴이 없다면 새 메시지만 체크해서 시작
            if (!IsDisplayingMessages)
            {
                if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
                messageDisplayCoroutine = StartCoroutine(ShowUnreadMessagesCoroutine(false)); // false: 초기화 안함
            }
        }
        else
        {
            OpenchatPartnerList();
        }
        ReportAlarmState();
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
        scrollRect.verticalNormalizedPosition = 0f;
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
        // 화면 초기화 (처음 방에 들어올 때만 실행)
        foreach (Transform child in chatRoomContent) Destroy(child.gameObject);

        if (currentChat == null) return;

        int lastSeenIndex = GetLastSeenIndex(currentChat);

        // 과거에 이미 봤던 메시지들만 즉시 생성
        for (int i = 0; i <= lastSeenIndex; i++)
        {
            if (i < currentChat.messages.Count)
                CreateMessageBubble(currentChat.messages[i].messageText);
        }

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        // 처음 방에 들어왔으니 첫 메시지는 즉시 출력하도록 설정
        messageDisplayCoroutine = StartCoroutine(ShowUnreadMessagesCoroutine(true));
    }

    private IEnumerator ShowUnreadMessagesCoroutine(bool isFreshEntry)
    {
        IsDisplayingMessages = true;

        if (currentChat == null)
        {
            IsDisplayingMessages = false;
            yield break;
        }

        // 방에 처음 들어온 상태가 아니라면(이미 방을 보고 있는데 새 메시지가 온 거라면) 
        // 첫 메시지도 딜레이를 가져야 하므로 false로 시작
        bool isFirstMessageInThisSession = isFreshEntry;

        int i = GetLastSeenIndex(currentChat) + 1;

        while (i < currentChat.messages.Count)
        {
            ChatMessage message = currentChat.messages[i];

            if (progress.activatedTriggers.Contains(message.triggerId))
            {
                // 딜레이 적용 로직
                if (isFirstMessageInThisSession)
                {
                    // 방에 처음 들어와서 과거 안읽은걸 뿌릴 때는 첫 메시지만 즉시 출력
                    isFirstMessageInThisSession = false;
                }
                else
                {
                    // 이미 방을 보고 있는 상태에서 새 메시지가 추가된 거라면 딜레이와 타이핑 효과 적용
                    yield return new WaitForSeconds(message.delayAfterPrevious / 2);
                    mc.AddTypingMessage(message.delayAfterPrevious/2);
                    yield return new WaitForSeconds(message.delayAfterPrevious / 2);
                }

                if (currentChat == null) break;

                CreateMessageBubble(message.messageText);
                SetLastSeenIndex(currentChat, i);
                ReportAlarmState();

                yield return null;
                scrollRect.verticalNormalizedPosition = 0f;

                i++; 
            }
            else
            {
                break;
            }
        }

        IsDisplayingMessages = false;
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

    public MessengerProgress GetProgress()
    {
        return progress;
    }

    public void SetProgress(MessengerProgress pro)
    {
        progress = pro;
        UpdateMessenger();
    }

    public bool IsTriggerFullySeen(string triggerId)
    {
        foreach (var chat in allChats)
        {
            // 1. 이 채팅방에서 해당 트리거 아이디를 사용하는 '가장 마지막 메시지'의 인덱스를 찾습니다.
            int lastTargetIndex = -1;
            for (int i = 0; i < chat.messages.Count; i++)
            {
                if (chat.messages[i].triggerId == triggerId)
                {
                    lastTargetIndex = i;
                }
            }

            // 해당 채팅방에 이 트리거가 없다면 다음 채팅방으로
            if (lastTargetIndex == -1) continue;

            // 2. 현재 저장된 진행도(LastSeenIndex)와 비교합니다.
            int currentSeenIndex = GetLastSeenIndex(chat);

            // 마지막 메시지 인덱스보다 현재 본 인덱스가 작다면 아직 다 안 본 것임
            if (currentSeenIndex < lastTargetIndex)
            {
                return false;
            }
        }

        // 모든 채팅방을 검사했는데 미진행된 트리거 메시지가 없다면 완료된 것임
        return true;
    }
}
