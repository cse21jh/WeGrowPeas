using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using DG.Tweening;

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

    [SerializeField] private TMP_Text chatRoomHeaderName;
    [SerializeField] private Image chatRoomHeaderImage;
    [SerializeField] private ChatMessageList chatMessageList;
    [SerializeField] private ScrollRect scrollRect;


    [SerializeField] private GameObject chatPartnerListItemPrefab;

    [Header("Open/Close Animation Settings")]
    [SerializeField] private RectTransform Panel_OpenRect;
    [SerializeField] private RectTransform Panel_CloseRect;

    [SerializeField] private RectTransform PeoplePanel;
    [SerializeField] private RectTransform ChatPanel;

    [SerializeField] private float OpenCloseDuration = 0.5f;
    [SerializeField] private Ease OpenCloseEase = Ease.InOutSine;

    private MessengerProgress progress;
    private Chat currentChat;
    private Coroutine messageDisplayCoroutine;

    private bool closedByTabShowingChat = false;
    private bool alreadyOpenChatRoom = false;
    public bool IsDisplayingMessages { get; private set; } = false;

    // 타이핑 스킵 플래그 (클릭 시 현재 "..." 대기를 즉시 건너뜀)
    private bool _skipTyping = false;



    void Awake()
    {
        progress = new MessengerProgress();

        //Trigger 0인 애들은 이미 와있던 메세지로 취급하고 삽입해줌. (New Game인 경우)
        if (progress.conversationSeenIndices.Count == 0 && progress.activatedTriggersOrdered.Count == 0) // 더 확실한 새 게임 확인
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
                    MessengerSaveSystem.MarkAsRead(partnerName, lastIndexOfTriggerZero);

                    if (!progress.daySeparators.ContainsKey(partnerName))
                    {
                        progress.daySeparators[partnerName] = new Dictionary<int, int>();
                    }

                    for (int i = firstIndexOfTriggerZero; i <= lastIndexOfTriggerZero; i++)
                    {
                        if (chat.messages[i].triggerId == "Default")
                        {
                            progress.daySeparators[partnerName][i] = 1;
                        }
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

    void Update()
    {
        // 채팅방에서 메시지 표시 중 클릭하면 현재 타이핑 대기를 스킵
        if (IsDisplayingMessages && chatRoomPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            _skipTyping = true;
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

            int currentDay = (GameManager.Instance != null) ? GameManager.Instance.stage : 1;

            foreach (var chat in allChats)
            {
                if (chat == null || chat.messages == null) continue;
                string partnerName = chat.chatPartner.chatPartnerName;
                for (int i = 0; i < chat.messages.Count; i++)
                {
                    if (chat.messages[i].triggerId == triggerId)
                    {
                        if (!progress.daySeparators.ContainsKey(partnerName))
                            progress.daySeparators[partnerName] = new Dictionary<int, int>();
                        if (!progress.daySeparators[partnerName].ContainsKey(i))
                            progress.daySeparators[partnerName][i] = currentDay;
                    }
                }
            }
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


    private Tween currentPanelTween;

    public void OpenchatPartnerList()
    {
        if (PhoneManager.Instance.isTutorial && PhoneManager.Instance.isTutorialEnd)
            return;

        if (messageDisplayCoroutine != null) StopCoroutine(messageDisplayCoroutine);
        IsDisplayingMessages = false;

        chatPartnerListPanel.SetActive(true); // 이동 시작 전에 미리 켜서 갱신이 동작하도록 함
        PeoplePanel.gameObject.SetActive(true);

        if (currentPanelTween != null) currentPanelTween.Kill();
        currentPanelTween = ChatPanel.DOAnchorPosX(Panel_CloseRect.anchoredPosition.x, OpenCloseDuration)
            .SetEase(OpenCloseEase)
            .OnComplete(() =>
            {
                chatRoomPanel.SetActive(false); // 이동 완료 후 가림
            });

        alreadyOpenChatRoom = false;
        currentChat = null;
        RefreshchatPartnerList();
    }


    public void OpenChatRoom(Chat conversation)
    {
        currentChat = conversation;

        chatRoomHeaderName.text = conversation.chatPartner.chatPartnerName;
        chatRoomHeaderImage.sprite = conversation.chatPartner.chatPartnerImage;
        chatMessageList.SetSender(conversation.chatPartner.chatPartnerName, conversation.chatPartner.chatPartnerImage);

        chatRoomPanel.SetActive(true); // 이동 시작 전에 미리 켜서 코루틴(Scroll View 등)이 동작하도록 함
        ChatPanel.gameObject.SetActive(true);

        if (currentPanelTween != null) currentPanelTween.Kill();
        currentPanelTween = ChatPanel.DOAnchorPosX(Panel_OpenRect.anchoredPosition.x, OpenCloseDuration)
            .SetEase(OpenCloseEase)
            .OnComplete(() =>
            {
                chatPartnerListPanel.SetActive(false); // 이동 완료 후 가림
            });

        alreadyOpenChatRoom = true;
        DisplayChatMessages();
        scrollRect.verticalNormalizedPosition = 0f;
    }



    public void RefreshchatPartnerList()
    {
        foreach (Transform child in chatPartnerListContent) Destroy(child.gameObject);

        List<Chat> chatsToShow = allChats
            .Where(chat => chat != null && chat.messages != null && HasAnyArrivedMessages(chat))
            .ToList();

        List<Chat> sortedChats = chatsToShow.OrderByDescending(chat =>
        {
            List<ChatMessage> arrivedMessages = new List<ChatMessage>();
            foreach (string triggerId in progress.activatedTriggersOrdered)
            {
                arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
            }

            if (arrivedMessages.Count == 0)
            {
                return -1;
            }

            ChatMessage lastMessage = arrivedMessages.Last();
            int originalIndex = chat.messages.IndexOf(lastMessage);
            return GetMessageDay(chat.chatPartner.chatPartnerName, originalIndex, lastMessage.triggerId);

        }).ToList();


        foreach (var chat in sortedChats)
        {
            UnreadInfo unreadInfo = GetUnreadInfo(chat);
            int cnt = 0;
            string previewMessage = GetPreviewMessageText(chat, unreadInfo.hasUnread, out cnt);

            GameObject itemGO = Instantiate(chatPartnerListItemPrefab, chatPartnerListContent);
            ChatPartnerUI uiComponent = itemGO.GetComponent<ChatPartnerUI>();
            if (uiComponent != null)
            {
                uiComponent.Setup(chat, previewMessage, unreadInfo, this, cnt);
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

            if (lastSeenPositionInArrivedList == -1)
            {
                for (int i = arrivedMessages.Count - 1; i >= 0; i--)
                {
                    if (chat.messages.IndexOf(arrivedMessages[i]) <= lastSeenIndex)
                    {
                        lastSeenPositionInArrivedList = i;
                        break;
                    }
                }
            }
        }

        // 3. 아직 안 본 메시지가 있는지 확인
        if (arrivedMessages.Count > lastSeenPositionInArrivedList + 1)
        {
            info.hasUnread = true;

            // 4. 안 읽은 메시지들 중에 진짜 안 읽은 '필수' 메시지가 있는지 확인
            var unreadMessages = arrivedMessages.Skip(lastSeenPositionInArrivedList + 1);
            string partnerName = chat.chatPartner.chatPartnerName;
            int profileSeenIndex = MessengerSaveSystem.GetLastSeenIndex(partnerName, chat.messages.Count);

            bool hasTrulyUnreadMandatory = false;
            foreach (var msg in unreadMessages)
            {
                if (msg.isMandatory)
                {
                    int msgIndex = chat.messages.IndexOf(msg);
                    if (msgIndex > profileSeenIndex)
                    {
                        hasTrulyUnreadMandatory = true;
                        break;
                    }
                }
            }

            if (hasTrulyUnreadMandatory)
            {
                info.hasMandatory = true;
            }
        }
        // --- 수정된 로직 끝 ---

        return info;
    }

    private string GetPreviewMessageText(Chat chat, bool hasUnread, out int cnt)
    {
        cnt = 0;
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
        string partnerName = chat.chatPartner.chatPartnerName;

        // 2. 도착한 날짜가 '오늘'인 안 읽은 메시지 확인
        var todayMessages = unreadMessages.Where(msg =>
        {
            int originalIndex = chat.messages.IndexOf(msg);
            int arrivalDay = GetMessageDay(partnerName, originalIndex, msg.triggerId);
            return arrivalDay == currentDay;
        }).ToList();

        if (todayMessages.Any())
        {
            // 오늘 날짜 메시지가 있다면, 그중 가장 첫 번째 메시지를 보여주고 카운트는 1
            cnt = 1;
            return todayMessages.First().messageText;
        }

        // 1. 도착한 날짜가 '과거'인 안 읽은 메시지 확인
        var pastDayMessages = unreadMessages.Where(msg =>
        {
            int originalIndex = chat.messages.IndexOf(msg);
            int arrivalDay = GetMessageDay(partnerName, originalIndex, msg.triggerId);
            return arrivalDay < currentDay;
        }).ToList();

        if (pastDayMessages.Any())
        {
            // 과거 날짜 메시지가 있다면, 그중 가장 마지막 메시지를 보여주고 카운트는 전체 누적 개수
            cnt = unreadMessages.Count;
            return pastDayMessages.Last().messageText;
        }

        // 안전장치로, 안 읽은 메시지 중 가장 첫 번째 것을 반환
        if (unreadMessages.Any())
        {
            cnt = 1; // 기본적으로 당일 도착으로 간주
            return unreadMessages.First().messageText;
        }

        // 정말 아무것도 해당하지 않는 예외적인 경우
        return arrivedMessages.Last().messageText;
    }


    private void DisplayChatMessages()
    {
        chatMessageList.ClearMessages();
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
                if (positionInShowList > -1)
                {
                    seenMessageCount = positionInShowList + 1;
                }
                else
                {
                    int fallbackPos = -1;
                    for (int i = messagesToShow.Count - 1; i >= 0; i--)
                    {
                        if (currentChat.messages.IndexOf(messagesToShow[i]) <= lastSeenIndex)
                        {
                            fallbackPos = i;
                            break;
                        }
                    }
                    if (fallbackPos > -1) seenMessageCount = fallbackPos + 1;
                }
            }
            else
            {
                seenMessageCount = messagesToShow.Count;
            }
        }

        // 3. 이미 본 메시지들을 즉시 표시 (채팅방에 처음 들어왔을 때만)
        if (isFreshEntry)
        {
            // 화면을 깨끗이 비운다
            chatMessageList.ClearMessages();

            int lastDisplayedDay = -1;
            for (int i = 0; i < seenMessageCount; i++)
            {
                int originalIndex = currentChat.messages.IndexOf(messagesToShow[i]);
                int day = GetMessageDay(partnerName, originalIndex, messagesToShow[i].triggerId);

                if (!progress.daySeparators.ContainsKey(partnerName))
                    progress.daySeparators[partnerName] = new Dictionary<int, int>();
                progress.daySeparators[partnerName][originalIndex] = day;

                if (day != -1 && lastDisplayedDay != day && !PhoneManager.Instance.isTutorial)
                {
                    lastDisplayedDay = day;
                }
                CreateMessageBubble(day, messagesToShow[i].messageText, messagesToShow[i].triggerId);
            }
        }

        // 4. 아직 안 본 메시지들을 이어서 표시
        bool isFirstUnreadInThisSession = isFreshEntry;
        bool afterPast = false;
        bool skippedFirstToday = false;

        int lastDay = -1;
        if (seenMessageCount > 0)
        {
            ChatMessage lastSeenMsg = messagesToShow[seenMessageCount - 1];
            int lastSeenOrigIndex = currentChat.messages.IndexOf(lastSeenMsg);
            lastDay = GetMessageDay(partnerName, lastSeenOrigIndex, lastSeenMsg.triggerId);
        }

        for (int i = seenMessageCount; i < messagesToShow.Count; i++)
        {
            ChatMessage message = messagesToShow[i];
            int originalIndex = currentChat.messages.IndexOf(message);
            int currentDay = GetMessageDay(partnerName, originalIndex, message.triggerId);

            // --- 날짜 변경 감지 및 저장/표시 로직 ---
            if (lastDay != currentDay && !PhoneManager.Instance.isTutorial)
            {
                if (!progress.daySeparators.ContainsKey(partnerName))
                    progress.daySeparators[partnerName] = new Dictionary<int, int>();
                progress.daySeparators[partnerName][originalIndex] = currentDay;
                // TODO: 세이브
                lastDay = currentDay;
            }
            bool isPast = IsPastDayMessage(partnerName, originalIndex, message.triggerId);

            if (isFirstUnreadInThisSession && isPast)
            {
                // 과거, 처음 읽음
                if (isFreshEntry)
                    chatMessageList.AddNewChatSeparator();
                isFirstUnreadInThisSession = false;
                afterPast = true;
            }
            else if (isFirstUnreadInThisSession)
            {
                // 세션의 첫 안읽은 메시지이지만, '오늘' 날짜라면 분리선만 표시
                if (isFreshEntry)
                    chatMessageList.AddNewChatSeparator();
                isFirstUnreadInThisSession = false;
            }
            else if (!isFirstUnreadInThisSession && afterPast && !isPast)
            {
                afterPast = false;
            }

            if (!isPast)
            {
                if (!skippedFirstToday)
                {
                    // 당일 도착 메시지 중 첫 번째(미리보기로 본 메시지)는 타이핑 연출 없이 즉시 출력
                    skippedFirstToday = true;
                }
                else
                {
                    float time = (message.messageText.Length / 10f);
                    float preDelay = time / 3f;
                    float typingDelay = time - preDelay;

                    // 메시지 전 짧은 대기 (클릭 시 스킵)
                    _skipTyping = false;
                    float elapsed = 0f;
                    while (elapsed < preDelay && !_skipTyping)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    // "..." 버블 표시 후 대기 (클릭 시 즉시 제거)
                    if (!_skipTyping)
                    {
                        chatMessageList.ShowTypingMessage();
                        elapsed = 0f;
                        while (elapsed < typingDelay && !_skipTyping)
                        {
                            elapsed += Time.deltaTime;
                            yield return null;
                        }
                    }

                    // 스킵되거나 시간이 끝나면 "..." 버블 제거
                    chatMessageList.HideTypingMessage();
                    _skipTyping = false;
                }
            }

            if (currentChat == null) break;

            CreateMessageBubble(currentDay, message.messageText, message.triggerId);
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
    private bool IsPastDayMessage(string partnerName, int originalIndex, string triggerId)
    {
        int arrivalDay = GetMessageDay(partnerName, originalIndex, triggerId);
        int currentDay = (GameManager.Instance != null) ? GameManager.Instance.stage : 1;
        return arrivalDay < currentDay;
    }

    private int GetMessageDay(string partnerName, int originalIndex, string triggerId)
    {
        if (progress.daySeparators.ContainsKey(partnerName) && progress.daySeparators[partnerName].TryGetValue(originalIndex, out int savedDay))
        {
            return savedDay;
        }

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

    private void CreateMessageBubble(int stage, string text, string triggerId = null)
    {
        int stageToPass = stage;
        if (PhoneManager.Instance != null && PhoneManager.Instance.isTutorial && !string.IsNullOrEmpty(triggerId))
        {
            stageToPass = triggerId.GetHashCode();
        }
        chatMessageList.AddMessage(stageToPass, text);
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

        if (lastSeenPositionInArrivedList == -1)
        {
            for (int i = arrivedMessages.Count - 1; i >= 0; i--)
            {
                if (chat.messages.IndexOf(arrivedMessages[i]) <= lastSeenIndex)
                {
                    lastSeenPositionInArrivedList = i;
                    break;
                }
            }
        }

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
        else if (MessengerSaveSystem.PlayAlarmForSeenMessages && HasReadMandatoryMessages())
        {
            currentState = AlarmState.NonMandatory;
        }
        else if (HasUnreadMessagesForAllChats()) // 모든 채팅방 중 하나라도 안 읽은 게 있다면
        {
            currentState = AlarmState.NonMandatory;
        }
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Messenger, currentState);
    }

    private bool HasReadMandatoryMessages()
    {
        foreach (var chat in allChats)
        {
            if (HasReadMandatoryMessagesInChat(chat))
                return true;
        }
        return false;
    }

    private bool HasReadMandatoryMessagesInChat(Chat chat)
    {
        if (chat == null || chat.messages == null) return false;

        List<ChatMessage> arrivedMessages = new List<ChatMessage>();
        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            arrivedMessages.AddRange(chat.messages.Where(msg => msg.triggerId == triggerId));
        }

        string partnerName = chat.chatPartner.chatPartnerName;
        int profileSeenIndex = MessengerSaveSystem.GetLastSeenIndex(partnerName, chat.messages.Count);

        int slotSeenIndex = -1;
        if (progress.conversationSeenIndices.TryGetValue(partnerName, out int index))
        {
            slotSeenIndex = index;
        }

        foreach (var msg in arrivedMessages)
        {
            int originalIndex = chat.messages.IndexOf(msg);
            if (originalIndex > slotSeenIndex && originalIndex <= profileSeenIndex && msg.isMandatory)
            {
                return true;
            }
        }
        return false;
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

        // 2. 프로필 전체의 읽음 인덱스 확인 (진짜 안 읽은 필수 메시지 감지용)
        string partnerName = chat.chatPartner.chatPartnerName;
        int profileSeenIndex = MessengerSaveSystem.GetLastSeenIndex(partnerName, chat.messages.Count);
        if (profileSeenIndex == -1) // 한 번도 안 봤다면
        {
            // 도착한 메시지 중에 필수 메시지가 있는지 확인
            return arrivedMessages.Any(msg => msg.isMandatory);
        }

        if (profileSeenIndex >= chat.messages.Count)
        {
            return false;
        }

        ChatMessage profileSeenMessage = chat.messages[profileSeenIndex];
        int positionInArrivedList = arrivedMessages.IndexOf(profileSeenMessage);

        if (positionInArrivedList == -1)
        {
            for (int i = arrivedMessages.Count - 1; i >= 0; i--)
            {
                if (chat.messages.IndexOf(arrivedMessages[i]) <= profileSeenIndex)
                {
                    positionInArrivedList = i;
                    break;
                }
            }
        }
        if (positionInArrivedList == -1) return false;

        // 3. 아직 프로필 기준으로도 안 본 메시지들 중에 필수 메시지가 있는지 확인
        return arrivedMessages
            .Skip(positionInArrivedList + 1)
            .Any(msg => msg.isMandatory);
    }

    private int GetLastSeenIndex(Chat chat)
    {
        if (chat == null || chat.chatPartner == null) return -1;

        string partnerName = chat.chatPartner.chatPartnerName;
        int profileSeenIndex = MessengerSaveSystem.GetLastSeenIndex(partnerName, chat.messages.Count);

        int slotSeenIndex = -1;
        if (progress.conversationSeenIndices.TryGetValue(partnerName, out int index))
        {
            slotSeenIndex = index;
        }

        // 설정이 꺼진 경우: 이미 읽은 경우 메신저 자체에서도 읽은 판정
        if (!MessengerSaveSystem.PlayAlarmForSeenMessages)
        {
            if (slotSeenIndex > profileSeenIndex)
            {
                MessengerSaveSystem.MarkAsRead(partnerName, slotSeenIndex);
                return slotSeenIndex;
            }
            return profileSeenIndex;
        }

        // 설정이 켜진 경우: 이미 프로필에서 읽었더라도 슬롯 기준 아직 안 읽었다면 새로 온 판정(Unread)으로 처리
        return slotSeenIndex;
    }

    private void SetLastSeenIndex(Chat chat, int index)
    {
        string partnerName = chat.chatPartner.chatPartnerName;
        progress.conversationSeenIndices[partnerName] = index;
        MessengerSaveSystem.MarkAsRead(partnerName, index);
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
        if (!open && alreadyOpenChatRoom == true)
        {
            if (!closedByTabShowingChat)
                doubleClose = true;

            if (messageDisplayCoroutine != null)
            {
                StopCoroutine(messageDisplayCoroutine);
                IsDisplayingMessages = false;
            }
            closedByTabShowingChat = true;
        }
        if (closedByTabShowingChat && open && alreadyOpenChatRoom == true)
        {
            if (doubleClose == true)
            {
                doubleClose = false;
                scrollRect.verticalNormalizedPosition = 1f;
                OpenChatRoom(currentChat);
                return;
            }
            scrollRect.verticalNormalizedPosition = 1f;
            OpenChatRoom(currentChat);
            closedByTabShowingChat = false;
        }
    }
}
