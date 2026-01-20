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
    public List<string> activatedTriggersOrdered = new List<string>();

    public Dictionary<string, Dictionary<int, int>> daySeparators = new Dictionary<string, Dictionary<int, int>>();
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

    private Dictionary<string, int> lastMessageDay = new Dictionary<string, int>();

    private bool closedByTabShowingChat = false;

    public bool IsDisplayingMessages { get; private set; } = false;

    void Awake()
    {
        progress = new MessengerProgress();

        //Trigger 0인 애들은 이미 와있던 메세지로 취급하고 삽입해줌. (New Game인 경우)
        if(progress.conversationSeenIndices.Count == 0 && progress.activatedTriggersOrdered.Count == 0) // 더 확실한 새 게임 확인
        {
            InitializeForNewGame();
        }
    }

    private void InitializeForNewGame()
    {
        if (allChats == null) return;

        // Trigger "Default"을 가장 먼저 활성화된 것으로 기록
        progress.activatedTriggersOrdered.Add("Default");        


        foreach (var chat in allChats)
        {
            if (chat != null && chat.chatPartner != null)
            {
                string partnerName = chat.chatPartner.chatPartnerName;

                // 딕셔너리에 대화 상대 추가
                if (!progress.conversationSeenIndices.ContainsKey(partnerName))
                {
                    progress.conversationSeenIndices.Add(partnerName, -1);
                }

                int lastIndexOfTriggerZero = -1;
                int firstIndexOfTriggerZero = -1; 

                for (int i = 0; i < chat.messages.Count; i++)
                {
                    if (chat.messages[i].triggerId == "Default")
                    {
                        if (firstIndexOfTriggerZero == -1) 
                        {
                            firstIndexOfTriggerZero = i;
                        }
                        lastIndexOfTriggerZero = i; 
                    }
                }

                // Trigger "0" 메시지가 있다면, "이미 읽음" 처리
                if (lastIndexOfTriggerZero > -1)
                {
                    progress.conversationSeenIndices[partnerName] = lastIndexOfTriggerZero;

                    if (!progress.daySeparators.ContainsKey(partnerName))
                    {
                        progress.daySeparators[partnerName] = new Dictionary<int, int>();
                    }

                    // 방어 코드
                    if (!progress.daySeparators[partnerName].ContainsKey(firstIndexOfTriggerZero))
                    {
                        progress.daySeparators[partnerName].Add(firstIndexOfTriggerZero, 0);
                    }
                }
            }
        }
        RefreshchatPartnerList();
    }

    void Start()
    {
        if (backTochatPartnersButton != null)
        { 
            backTochatPartnersButton.onClick.AddListener(OpenchatPartnerList);
            backTochatPartnersButton.onClick.AddListener(PhoneManager.Instance.PhoneTouchEffect);
        }
    }    

    public void UpdateMessenger()
    {
        if (chatRoomPanel.activeSelf && currentChat != null)
        {
            // 핵심 변경: 화면을 다 지우지 않고, 현재 진행 중인 코루틴이 없다면 새 메시지만 체크해서 시작
            if (!IsDisplayingMessages)
            {
                if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
                messageDisplayCoroutine = StartCoroutine(ShowMessagesInOrderCoroutine(false)); // false: 초기화 안함
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
        if (!DoesAnyMessageUseTrigger(triggerId)) return;

        // 중복 추가 방지
        if (!progress.activatedTriggersOrdered.Contains(triggerId))
        {
            progress.activatedTriggersOrdered.Add(triggerId);
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

        // --- 여기가 수정된 로직 ---
        // 1. 이 채팅방에서 도착한 모든 메시지를 도착 순서대로 정렬
        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }

        if (arrivedMessages.Count == 0) return info;

        // 2. 마지막으로 본 메시지가 도착한 메시지 리스트에서 몇 번째인지 확인
        int lastSeenIndex = GetLastSeenIndex(chat);
        int lastSeenPositionInArrivedList = -1; // -1은 한 번도 안 봤다는 의미

        if (lastSeenIndex > -1)
        {
            // lastSeenIndex가 유효한지 확인
            if (lastSeenIndex >= chat.messages.Count)
            {
                Debug.LogError($"Invalid lastSeenIndex ({lastSeenIndex}) for chat '{chat.name}'.");
                return info; // 오류 상황에서는 안 읽은 메시지 없다고 처리
            }
            ChatMessage lastSeenMessage = chat.messages[lastSeenIndex];
            lastSeenPositionInArrivedList = arrivedMessages.IndexOf(lastSeenMessage);
        }

        // 3. 아직 안 본 메시지가 있는지 확인
        if (arrivedMessages.Count > lastSeenPositionInArrivedList + 1)
        {
            info.hasUnread = true;

            // 4. 안 읽은 메시지들 중에 '필수' 메시지가 있는지 확인
            var unreadMessages = arrivedMessages.Skip(lastSeenPositionInArrivedList + 1);
            if (unreadMessages.Any(msg => msg.isMandatory))
            {
                info.hasMandatory = true;
            }
        }
        // --- 수정된 로직 끝 ---

        return info;
    }

    private string GetPreviewMessageText(Chat chat, bool hasUnread)
    {
        if (chat == null || chat.messages == null || chat.messages.Count == 0) return "새로운 대화";

        // 1. 이 채팅방에서 도착한 모든 메시지를 도착 순서대로 정렬
        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }

        if (arrivedMessages.Count == 0) return "새로운 대화";

        if (hasUnread)
        {
            // 2. 마지막으로 본 메시지의 위치 확인
            int lastSeenIndex = GetLastSeenIndex(chat);
            int lastSeenPositionInArrivedList = -1;
            if (lastSeenIndex > -1)
            {
                lastSeenPositionInArrivedList = arrivedMessages.IndexOf(chat.messages[lastSeenIndex]);
            }

            // 3. 안 읽은 메시지 중 첫 번째 메시지 반환
            int nextMessageIndex = lastSeenPositionInArrivedList + 1;
            if (nextMessageIndex < arrivedMessages.Count)
            {
                return arrivedMessages[nextMessageIndex].messageText;
            }
        }

        // 4. 안 읽은 메시지가 없는 경우: 마지막으로 도착한 메시지를 반환 (더 자연스러움)
        return arrivedMessages.Last().messageText;
    }

    private void DisplayChatMessages()
    {
        foreach (Transform child in chatRoomContent) Destroy(child.gameObject);
        if (currentChat == null) return;

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        messageDisplayCoroutine = StartCoroutine(ShowMessagesInOrderCoroutine(true));
    }


    private IEnumerator ShowMessagesInOrderCoroutine(bool isFreshEntry)
    {
        IsDisplayingMessages = true;
        if (currentChat == null) { IsDisplayingMessages = false; yield break; }

        bool isFirstMessageInSession = isFreshEntry;
        string partnerName = currentChat.chatPartner.chatPartnerName;

        // 1. 보여줘야 할 모든 메시지를 '도착한 순서대로' 정렬
        List<ChatMessage> messagesToShow = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            messagesToShow.AddRange(currentChat.messages.Where(msg => msg.triggerId == triggerId));
        }

        // 2. "몇 개까지 봤는지" 계산
        int seenMessageCount = 0;
        int lastSeenIndex = GetLastSeenIndex(currentChat);
        if (lastSeenIndex > -1)
        {
            ChatMessage lastSeenMessage = currentChat.messages[lastSeenIndex];
            int positionInShowList = messagesToShow.IndexOf(lastSeenMessage);
            if (positionInShowList > -1) seenMessageCount = positionInShowList + 1;
        }

        // 3. 이미 본 메시지들을 즉시 표시 (저장된 날짜 구분선 정보 사용)
        for (int i = 0; i < seenMessageCount; i++)
        {
            if (isFreshEntry == false)
                break;
            // 현재 메시지의 원본 인덱스
            int originalIndex = currentChat.messages.IndexOf(messagesToShow[i]);

            // 이 메시지 앞에 날짜 구분선이 있는지 확인
            if (progress.daySeparators.ContainsKey(partnerName) && progress.daySeparators[partnerName].ContainsKey(originalIndex))
            {
                mc.AddDay(progress.daySeparators[partnerName][originalIndex]);
            }
            CreateMessageBubble(messagesToShow[i].messageText);
        }

        // 4. 아직 안 본 메시지들을 이어서 표시 (날짜 변경 감지 및 저장)
        for (int i = seenMessageCount; i < messagesToShow.Count; i++)
        {
            ChatMessage message = messagesToShow[i];
            int originalIndex = currentChat.messages.IndexOf(message);
            int currentDay = 1;
            if (GameManager.Instance != null)
                currentDay = GameManager.Instance.stage;

            // *** 날짜 변경 감지 및 저장 로직 ***
            if (!lastMessageDay.ContainsKey(partnerName))
            {
                // 이 대화 상대의 첫 메시지라면, 현재 날짜를 기록
                lastMessageDay[partnerName] = -1; // -1로 초기화하여 첫 메시지 앞에 날짜가 뜨도록 함
            }

            if (lastMessageDay[partnerName] != currentDay)
            {
                // 날짜가 변경되었다면
                mc.AddDay(currentDay); // 날짜 알림 표시

                // 이 정보를 progress에 영구적으로 저장
                if (!progress.daySeparators.ContainsKey(partnerName))
                {
                    progress.daySeparators[partnerName] = new Dictionary<int, int>();
                }
                progress.daySeparators[partnerName][originalIndex] = currentDay;
                // TODO: 세이브

                lastMessageDay[partnerName] = currentDay; // 마지막 날짜 갱신
                yield return new WaitForSeconds(0.5f); // 날짜 표시 후 잠시 대기
            }

            // 딜레이 적용
            if (isFirstMessageInSession)
            {
                mc.AddNewChatSeperator();
                isFirstMessageInSession = false;
            }
            else
            {
                yield return new WaitForSeconds(message.delayAfterPrevious / 2);
                mc.AddTypingMessage(message.delayAfterPrevious / 2);
                yield return new WaitForSeconds(message.delayAfterPrevious / 2);
            }
            if (currentChat == null) break;

            CreateMessageBubble(message.messageText);
            SetLastSeenIndex(currentChat, originalIndex);

            ReportAlarmState();
            yield return null;
            scrollRect.verticalNormalizedPosition = 0f;
        }
        IsDisplayingMessages = false;
        messageDisplayCoroutine = null;
    }

    private void CreateMessageBubble(string text)
    {
        mc.AddMessage(MessageController.MessageSenderType.pea, text);
    }


    private bool HasAnyArrivedMessages(Chat chat)
    {
        if (chat == null || chat.messages == null) return false;
        // 도착한 트리거 중 이 채팅에 해당하는 메시지가 하나라도 있는지 확인
        return progress.activatedTriggersOrdered.Any(triggerId => chat.messages.Any(msg => msg.triggerId == triggerId));
    }

    private bool HasUnreadMessages(Chat chat)
    {
        if (chat == null || chat.messages == null) return false;

        // 1. 이 채팅방에서 도착한 모든 메시지를 도착 순서대로 정렬
        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }

        // 2. 마지막으로 본 메시지가 도착한 메시지 리스트의 몇 번째인지 확인
        int lastSeenIndex = GetLastSeenIndex(chat);
        if (lastSeenIndex == -1) // 한 번도 안 봤다면
        {
            return arrivedMessages.Count > 0; // 도착한 메시지가 있으면 안 읽은 것임
        }

        if (lastSeenIndex >= chat.messages.Count)
        {
            Debug.LogError("Invalid lastSeenIndex detected!");
            return false;
        }

        ChatMessage lastSeenMessage = chat.messages[lastSeenIndex];
        int lastSeenPositionInArrivedList = arrivedMessages.IndexOf(lastSeenMessage);

        // 3. 도착한 메시지 수와 마지막으로 본 메시지의 위치를 비교
        return arrivedMessages.Count > lastSeenPositionInArrivedList + 1;
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
            if (HasUnreadMandatoryMessagesInChat(chat))
                return true;
        }
        return false;
    }
    private bool HasUnreadMandatoryMessagesInChat(Chat chat)
    {
        if (chat == null || chat.messages == null) return false;

        // 1. 이 채팅방에서 도착한 모든 메시지를 도착 순서대로 정렬
        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }

        // 2. 마지막으로 본 메시지의 위치 확인
        int lastSeenIndex = GetLastSeenIndex(chat);
        if (lastSeenIndex == -1) // 한 번도 안 봤다면
        {
            // 도착한 메시지 중에 필수 메시지가 있는지 확인
            return arrivedMessages.Any(msg => msg.isMandatory);
        }

        if (lastSeenIndex >= chat.messages.Count)
        {
            Debug.LogError("Invalid lastSeenIndex detected!");
            return false;
        }

        ChatMessage lastSeenMessage = chat.messages[lastSeenIndex];
        int lastSeenPositionInArrivedList = arrivedMessages.IndexOf(lastSeenMessage);

        // 3. 아직 안 본 메시지들 중에 필수 메시지가 있는지 확인
        return arrivedMessages
            .Skip(lastSeenPositionInArrivedList + 1)
            .Any(msg => msg.isMandatory);
    }


    private string GetLastMessageText(Chat chat)
    {
        int lastSeenIndex = GetLastSeenIndex(chat);
        if (lastSeenIndex >= 0)
            return chat.messages[lastSeenIndex].messageText;

        var firstMessage = chat.messages.FirstOrDefault(msg => progress.activatedTriggersOrdered.Contains(msg.triggerId));
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
        RestoreLastMessageDays(); // lastMessageDay 상태 복원
        UpdateMessenger();
    }

    // *** lastMessageDay 상태를 복원하는 새로운 헬퍼 함수 ***
    private void RestoreLastMessageDays()
    {
        lastMessageDay.Clear();
        if (progress == null || progress.daySeparators == null) return;

        foreach (var chatPartnerEntry in progress.daySeparators)
        {
            string partnerName = chatPartnerEntry.Key;
            Dictionary<int, int> separators = chatPartnerEntry.Value;

            if (separators.Count > 0)
            {
                // 가장 마지막 인덱스(가장 최근)의 날짜 구분선 날짜를 가져와서 설정
                int latestDay = separators.OrderByDescending(kvp => kvp.Key).First().Value;
                lastMessageDay[partnerName] = latestDay;
            }
        }
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

    public void CheckCoroutineByTab(bool open)
    {
        if (messageDisplayCoroutine != null && !open)
        {            
            StopCoroutine(messageDisplayCoroutine);
            IsDisplayingMessages = false;
            closedByTabShowingChat = true;
        }
        if (closedByTabShowingChat && open)
        {
            OpenChatRoom(currentChat);
            closedByTabShowingChat = false;
        }
    }
}
