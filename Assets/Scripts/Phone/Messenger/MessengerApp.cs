using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    private bool closedByTabShowingChat = false;
    private bool alreadyOpenChatRoom = false;
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
            // 화면을 다 지우지 않고, 현재 진행 중인 코루틴이 없다면 새 메시지만 체크해서 시작
            if (!IsDisplayingMessages)
            {
                if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
                messageDisplayCoroutine = StartCoroutine(ShowMessagesInOrderCoroutine(false)); // false: 초기화 안함
            }
        }
        else
        {
            RefreshchatPartnerList();
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
            // TODO: 세이브
        }

        if (chatPartnerListPanel.activeInHierarchy || chatRoomPanel.activeInHierarchy)
        {
            UpdateMessenger();
        }
        else
        {
            RefreshchatPartnerList();
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
        if (PhoneManager.Instance.isTutorial && PhoneManager.Instance.isTutorialEnd)
            return;

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        IsDisplayingMessages = false;

        chatPartnerListPanel.SetActive(true);
        chatRoomPanel.SetActive(false);
        alreadyOpenChatRoom = false;
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

        alreadyOpenChatRoom = true;
        DisplayChatMessages();
        scrollRect.verticalNormalizedPosition = 0f;
    }



    public void RefreshchatPartnerList()
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

        // 도착한 모든 메시지를 도착 순서대로 정렬
        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }
        if (arrivedMessages.Count == 0) return "새로운 대화";

        // --- 안 읽은 메시지가 없는 경우 (가장 간단한 케이스) ---
        if (!hasUnread)
        {
            // 4. 가장 마지막으로 '도착한' 메시지를 반환
            return arrivedMessages.Last().messageText;
        }

        // --- 안 읽은 메시지가 있는 경우 ---
        int currentDay = (GameManager.Instance != null) ? GameManager.Instance.stage : 1;

        // 마지막으로 본 메시지의 위치 확인
        int lastSeenIndex = GetLastSeenIndex(chat);
        int lastSeenPositionInArrivedList = -1;
        if (lastSeenIndex > -1)
        {
            lastSeenPositionInArrivedList = arrivedMessages.IndexOf(chat.messages[lastSeenIndex]);
        }

        // 안 읽은 메시지 목록 생성
        List<ChatMessage> unreadMessages = arrivedMessages.Skip(lastSeenPositionInArrivedList + 1).ToList();        

        // 2. 오늘 날짜(숫자 트리거 == 현재 날짜) 알람이 있는지 확인
        var todayMessages = unreadMessages.Where(msg =>
        {
            if (int.TryParse(msg.triggerId, out int day) && day != 0)
                return day == currentDay;
            return false;
        }).ToList();

        if (todayMessages.Any())
        {
            // 오늘 날짜 메시지가 있다면,
            // 그중 가장 첫 번째 메시지를 보여준다.
            return todayMessages.First().messageText;
        }

        // 3. 날짜가 아닌 다른 트리거의 안 읽은 메시지가 있는지 확인
        var otherTriggerMessages = unreadMessages.Where(msg => !int.TryParse(msg.triggerId, out _)).ToList();

        if (otherTriggerMessages.Any())
        {
            // 그 외 다른 트리거 메시지가 있다면,
            // 그중 가장 첫 번째 메시지를 보여준다.
            return otherTriggerMessages.First().messageText;
        }

        // 1. 과거 날짜(숫자 트리거 < 현재 날짜) 알람이 있는지 확인
        var pastDayMessages = unreadMessages.Where(msg =>
        {
            if (int.TryParse(msg.triggerId, out int day) && day != 0)
                return day < currentDay;
            return false;
        }).ToList();

        if (pastDayMessages.Any())
        {
            // 과거 날짜 메시지가 있다면,
            // 그중 가장 마지막 메시지를 보여준다. (가장 마지막에 도착한 트리거의 마지막 메시지에 해당)
            return pastDayMessages.Last().messageText;
        }

        // 이론상 unreadMessages가 비어있지 않다면 이 지점까지 오지 않아야 함.
        // 안전장치로, 안 읽은 메시지 중 가장 첫 번째 것을 반환
        if (unreadMessages.Any())
        {
            return unreadMessages.First().messageText;
        }

        // 정말 아무것도 해당하지 않는 예외적인 경우
        return arrivedMessages.Last().messageText;
    }


    private void DisplayChatMessages()
    {
        foreach (Transform child in chatRoomContent) Destroy(child.gameObject);
        if (currentChat == null) return;

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        messageDisplayCoroutine = StartCoroutine(ShowMessagesInOrderCoroutine()); // isFreshEntry 파라미터 제거
    }



    private IEnumerator ShowMessagesInOrderCoroutine(bool isFreshEntry = true) // 기본값을 true로 설정
    {
        IsDisplayingMessages = true;
        if (currentChat == null) { IsDisplayingMessages = false; yield break; }

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
            if (lastSeenIndex < currentChat.messages.Count) // 방어 코드
            {
                ChatMessage lastSeenMessage = currentChat.messages[lastSeenIndex];
                int positionInShowList = messagesToShow.IndexOf(lastSeenMessage);
                if (positionInShowList > -1) seenMessageCount = positionInShowList + 1;
            }
        }

        // 3. 이미 본 메시지들을 즉시 표시 (채팅방에 처음 들어왔을 때만)
        if (isFreshEntry)
        {
            // 화면을 깨끗이 비운다
            foreach (Transform child in chatRoomContent) Destroy(child.gameObject);

            int lastDisplayedDay = -1;
            for (int i = 0; i < seenMessageCount; i++)
            {
                int originalIndex = currentChat.messages.IndexOf(messagesToShow[i]);
                if (progress.daySeparators.ContainsKey(partnerName) &&
                    progress.daySeparators[partnerName].ContainsKey(originalIndex))
                {
                    int day = progress.daySeparators[partnerName][originalIndex];
                    if (lastDisplayedDay != day)
                    {
                        mc.AddDay(day);
                        lastDisplayedDay = day;
                    }
                }
                CreateMessageBubble(messagesToShow[i].messageText);
            }
        }

        // 4. 아직 안 본 메시지들을 이어서 표시
        bool isFirstUnreadInThisSession = isFreshEntry;
        bool afterPast = false;
        int lastDay = GetLastDisplayedDayForChat(partnerName);

        if(seenMessageCount == messagesToShow.Count)
            mc.AddNewChatSeperator();

        for (int i = seenMessageCount; i < messagesToShow.Count; i++)
        {
            ChatMessage message = messagesToShow[i];
            int originalIndex = currentChat.messages.IndexOf(message);
            int currentDay = GetMessageDay(message.triggerId);

            // --- 날짜 변경 감지 및 저장/표시 로직 ---
            if (lastDay != currentDay && !PhoneManager.Instance.isTutorial)
            {
                mc.AddDay(currentDay);
                if (!progress.daySeparators.ContainsKey(partnerName))
                    progress.daySeparators[partnerName] = new Dictionary<int, int>();
                progress.daySeparators[partnerName][originalIndex] = currentDay;
                // TODO: 세이브
                lastDay = currentDay;
            }
            bool isPast = IsPastDayMessage(message.triggerId);

            if (isFirstUnreadInThisSession && isPast)
            {
                // 과거, 처음 읽음
                if(isFreshEntry)
                    mc.AddNewChatSeperator();
                isFirstUnreadInThisSession = false;
                afterPast = true;
            }
            else if (isFirstUnreadInThisSession)
            {
                // 세션의 첫 안읽은 메시지이지만, '오늘' 날짜라면 분리선만 표시
                if(isFreshEntry)
                    mc.AddNewChatSeperator();
                isFirstUnreadInThisSession = false;
            }
            else if (!isFirstUnreadInThisSession && afterPast && !isPast)
            {
                afterPast = false;
            }
            else if (!isPast)
            {
                float time = (message.messageText.Length / 10f);
                // 두 번째 이후의 안 읽은 메시지는 타이핑 효과 적용
                yield return new WaitForSeconds(time / 3);
                mc.AddTypingMessage(time - (time / 3));
                yield return new WaitForSeconds(time - (time / 3));
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

    private int GetLastDisplayedDayForChat(string partnerName)
    {
        // 2. 런타임 기록이 없다면 (예: 채팅방에 처음 들어왔을 때),
        //    저장된 progress 데이터에서 복원 시도
        if (progress != null && progress.daySeparators.TryGetValue(partnerName, out var separators) && separators.Count > 0)
        {
            // daySeparators에는 {메시지 인덱스, 날짜} 쌍이 저장되어 있음.
            // 이 중 가장 큰 메시지 인덱스(가장 최근)에 해당하는 날짜를 찾아서 반환.
            int latestDay = separators.OrderByDescending(kvp => kvp.Key).First().Value;

            return latestDay;
        }

        // 런타임 기록도, 저장된 기록도 없으면 -1 반환
        return -1;
    }
    private int GetMessageDay(string triggerId)
    {
        // int.TryParse는 문자열을 정수로 변환 시도, 성공하면 true 반환
        if (int.TryParse(triggerId, out int day) && day != 0)
        {
            return day; // 숫자로 변환 성공 시 해당 날짜 반환
        }

        // "Default" 또는 "Item_Acquired" 등 숫자가 아닌 트리거는 현재 날짜로 취급
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.stage;
        }
        return 1; // GameManager가 없을 경우 기본값
    }
    private bool IsPastDayMessage(string triggerId)
    {
        if (int.TryParse(triggerId, out int day) && day != 0)
        {
            int currentDay = (GameManager.Instance != null) ? GameManager.Instance.stage : 1;
            return day < currentDay;
        }        
        return false; // 숫자가 아닌 트리거는 과거 메시지로 취급하지 않음
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
            return false;
        }

        ChatMessage lastSeenMessage = chat.messages[lastSeenIndex];
        int lastSeenPositionInArrivedList = arrivedMessages.IndexOf(lastSeenMessage);

        // 3. 아직 안 본 메시지들 중에 필수 메시지가 있는지 확인
        return arrivedMessages
            .Skip(lastSeenPositionInArrivedList + 1)
            .Any(msg => msg.isMandatory);
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

    private bool doubleClose = false;
    public void CheckCoroutineByTab(bool open)
    {
        if (!open && alreadyOpenChatRoom == true )
        {
            if(!closedByTabShowingChat)
                doubleClose = true;

            if(messageDisplayCoroutine != null)
            { 
                StopCoroutine(messageDisplayCoroutine);
                IsDisplayingMessages = false;
            }            
            closedByTabShowingChat = true;
        }
        if (closedByTabShowingChat && open && alreadyOpenChatRoom == true)
        {
            if(doubleClose == true)
            {
                doubleClose = false;
                OpenChatRoom(currentChat);
                return;
            }
            OpenChatRoom(currentChat);
            closedByTabShowingChat = false;
        }
    }
}
