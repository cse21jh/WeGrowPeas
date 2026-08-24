using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessengerProgress
{
    public Dictionary<string, int> conversationSeenIndices = new Dictionary<string, int>();
    public Dictionary<string, HashSet<int>> revealedMessageIndices = new Dictionary<string, HashSet<int>>();
    public Dictionary<string, HashSet<int>> readMessageIndices = new Dictionary<string, HashSet<int>>();
    public List<string> activatedTriggersOrdered = new List<string>();
    public Dictionary<string, Dictionary<int, int>> daySeparators = new Dictionary<string, Dictionary<int, int>>();
}

public struct UnreadInfo
{
    public bool hasUnread;
    public bool hasMandatory;
}

public readonly struct MandatoryMessageHandle : IEquatable<MandatoryMessageHandle>
{
    public string PartnerName { get; }
    public int MessageIndex { get; }
    public string TriggerId { get; }

    public MandatoryMessageHandle(string partnerName, int messageIndex, string triggerId)
    {
        PartnerName = partnerName;
        MessageIndex = messageIndex;
        TriggerId = triggerId;
    }

    public bool Equals(MandatoryMessageHandle other)
    {
        return PartnerName == other.PartnerName
            && MessageIndex == other.MessageIndex
            && TriggerId == other.TriggerId;
    }

    public override bool Equals(object obj) => obj is MandatoryMessageHandle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(PartnerName, MessageIndex, TriggerId);
}

public class MessengerApp : MonoBehaviour
{
    private sealed class MessageReference
    {
        public Chat chat;
        public int index;
        public ChatMessage message;

        public MandatoryMessageHandle Handle => new MandatoryMessageHandle(
            chat.chatPartner.chatPartnerName,
            index,
            message.triggerId);
    }

    private sealed class MandatoryEntry
    {
        public MessageReference reference;
        public bool revealedByPopup;
        public bool advanceUnlocked;
        public bool waitEventRaised;
    }

    [Header("Data")]
    [SerializeField] private List<Chat> allChats;

    [Header("UI Panels")]
    [SerializeField] private GameObject chatPartnerListPanel;
    [SerializeField] private GameObject chatRoomPanel;
    [SerializeField] private MandatoryMessagePopupController mandatoryMessagePopup;

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

    private readonly List<MandatoryEntry> mandatoryEntries = new List<MandatoryEntry>();
    private readonly List<MessageReference> heldOptionalMessages = new List<MessageReference>();
    private readonly HashSet<int> renderedIndicesForCurrentChat = new HashSet<int>();

    private MessengerProgress progress;
    private Chat currentChat;
    private Coroutine messageDisplayCoroutine;
    private Tween currentPanelTween;
    private int mandatoryPopupIndex;
    private bool mandatoryPopupOpen;
    private bool messageRefreshPending;
    private bool closedByTabShowingChat;
    private bool alreadyOpenChatRoom;
    private bool doubleClose;
    private bool skipTyping;

    public bool IsDisplayingMessages { get; private set; }
    public bool IsMandatoryPopupOpen => mandatoryPopupOpen;
    public event Action<MandatoryMessageHandle> OnMandatoryAdvanceBlocked;

    private void Awake()
    {
        progress = new MessengerProgress();
        mandatoryMessagePopup?.Initialize(
            ShowPreviousMandatoryMessage,
            ShowNextMandatoryMessage,
            ConfirmMandatoryMessages);

        if (progress.activatedTriggersOrdered.Count == 0)
            InitializeForNewGame();
    }

    private void Start()
    {
        if (backTochatPartnersButton == null) return;
        backTochatPartnersButton.onClick.AddListener(OpenchatPartnerList);
        backTochatPartnersButton.onClick.AddListener(() => PhoneManager.Instance?.PhoneTouchEffect());
    }

    private void Update()
    {
        if (IsDisplayingMessages && chatRoomPanel != null && chatRoomPanel.activeInHierarchy
            && Input.GetMouseButtonDown(0))
        {
            skipTyping = true;
        }
    }

    private void InitializeForNewGame()
    {
        if (allChats == null) return;
        progress.activatedTriggersOrdered.Add("Default");

        foreach (MessageReference reference in GetReferencesForTrigger("Default"))
        {
            SetMessageDay(reference, 1);
            RevealMessage(reference);
            MarkMessageRead(reference.chat, reference.index);
        }

        RefreshchatPartnerList();
    }

    public void ActivateTrigger(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId) || !DoesAnyMessageUseTrigger(triggerId)) return;
        if (progress.activatedTriggersOrdered.Contains(triggerId)) return;

        progress.activatedTriggersOrdered.Add(triggerId);
        List<MessageReference> references = GetReferencesForTrigger(triggerId);
        int currentDay = GameManager.Instance != null ? GameManager.Instance.stage : 1;
        foreach (MessageReference reference in references)
            SetMessageDay(reference, currentDay);

        List<MessageReference> required = references
            .Where(reference => RequiresMandatoryPopup(reference.chat, reference.index))
            .ToList();

        if (required.Count == 0)
        {
            foreach (MessageReference reference in references)
                RevealMessage(reference);
            UpdateMessenger();
            return;
        }

        if (mandatoryMessagePopup == null)
        {
            Debug.LogError("[Messenger] MandatoryMessagePopupController가 연결되지 않아 필수 메시지를 자동 처리합니다.", this);
            foreach (MessageReference reference in references)
            {
                RevealMessage(reference);
                if (reference.message.isMandatory)
                    MarkMessageRead(reference.chat, reference.index);
            }
            UpdateMessenger();
            return;
        }

        bool startsNewSession = !mandatoryPopupOpen;
        if (startsNewSession)
        {
            mandatoryEntries.Clear();
            heldOptionalMessages.Clear();
            mandatoryPopupIndex = 0;
            mandatoryPopupOpen = true;
        }

        foreach (MessageReference reference in references)
        {
            if (required.Contains(reference))
                mandatoryEntries.Add(new MandatoryEntry { reference = reference });
            else if (!IsMessageRevealed(reference.chat, reference.index))
                heldOptionalMessages.Add(reference);
        }

        if (startsNewSession)
            ShowCurrentMandatoryMessage();
        else
            RefreshMandatoryNavigation();

        ReportAlarmState();
    }

    public bool TryGetAwaitingMandatoryAdvance(string triggerId, out MandatoryMessageHandle handle)
    {
        foreach (MandatoryEntry entry in mandatoryEntries)
        {
            if (entry.waitEventRaised && !entry.advanceUnlocked
                && entry.reference.message.waitForAdvanceSignal
                && entry.reference.message.triggerId == triggerId)
            {
                handle = entry.reference.Handle;
                return true;
            }
        }

        handle = default;
        return false;
    }

    public void UnlockMandatoryAdvance(MandatoryMessageHandle handle)
    {
        MandatoryEntry entry = mandatoryEntries.FirstOrDefault(item => item.reference.Handle.Equals(handle));
        if (entry == null) return;
        entry.advanceUnlocked = true;
        RefreshMandatoryNavigation();
    }

    private void ShowPreviousMandatoryMessage()
    {
        if (!mandatoryPopupOpen || mandatoryPopupIndex <= 0) return;
        mandatoryPopupIndex--;
        ShowCurrentMandatoryMessage();
    }

    private void ShowNextMandatoryMessage()
    {
        if (!mandatoryPopupOpen || mandatoryEntries.Count == 0) return;
        MandatoryEntry current = mandatoryEntries[mandatoryPopupIndex];
        if (current.reference.message.waitForAdvanceSignal && !current.advanceUnlocked) return;
        if (mandatoryPopupIndex >= mandatoryEntries.Count - 1) return;

        mandatoryPopupIndex++;
        ShowCurrentMandatoryMessage();
    }

    private void ConfirmMandatoryMessages()
    {
        if (!mandatoryPopupOpen || mandatoryEntries.Count == 0) return;
        if (mandatoryPopupIndex != mandatoryEntries.Count - 1) return;
        MandatoryEntry current = mandatoryEntries[mandatoryPopupIndex];
        if (current.reference.message.waitForAdvanceSignal && !current.advanceUnlocked) return;

        foreach (MessageReference reference in heldOptionalMessages)
            RevealMessage(reference);

        mandatoryPopupOpen = false;
        mandatoryMessagePopup.Hide();
        mandatoryEntries.Clear();
        heldOptionalMessages.Clear();
        mandatoryPopupIndex = 0;
        UpdateMessenger();
    }

    private void ShowCurrentMandatoryMessage()
    {
        if (!mandatoryPopupOpen || mandatoryEntries.Count == 0) return;
        mandatoryPopupIndex = Mathf.Clamp(mandatoryPopupIndex, 0, mandatoryEntries.Count - 1);

        MandatoryEntry entry = mandatoryEntries[mandatoryPopupIndex];
        MessageReference reference = entry.reference;
        mandatoryMessagePopup.Show(reference.chat.chatPartner, reference.message.messageText);

        if (!entry.revealedByPopup)
        {
            entry.revealedByPopup = true;
            RevealMessage(reference);
            MarkMessageRead(reference.chat, reference.index);
            UpdateMessenger();
        }

        if (reference.message.waitForAdvanceSignal && !entry.waitEventRaised)
        {
            entry.waitEventRaised = true;
            OnMandatoryAdvanceBlocked?.Invoke(reference.Handle);
        }

        RefreshMandatoryNavigation();
    }

    private void RefreshMandatoryNavigation()
    {
        if (!mandatoryPopupOpen || mandatoryEntries.Count == 0 || mandatoryMessagePopup == null) return;
        MandatoryEntry entry = mandatoryEntries[mandatoryPopupIndex];
        mandatoryMessagePopup.SetNavigation(
            mandatoryPopupIndex == 0,
            mandatoryPopupIndex == mandatoryEntries.Count - 1,
            entry.reference.message.waitForAdvanceSignal,
            entry.advanceUnlocked);
    }

    public void UpdateMessenger()
    {
        if (chatRoomPanel != null && chatRoomPanel.activeInHierarchy && currentChat != null)
        {
            if (IsDisplayingMessages)
                messageRefreshPending = true;
            else
                StartMessageDisplay(false);
        }
        else
        {
            RefreshchatPartnerList();
        }

        ReportAlarmState();
    }

    private bool DoesAnyMessageUseTrigger(string triggerId)
    {
        return allChats != null && allChats.Any(chat => chat != null && chat.messages != null
            && chat.messages.Any(message => message.triggerId == triggerId));
    }

    public void OpenchatPartnerList()
    {
        StopMessageDisplay();
        chatPartnerListPanel.SetActive(true);
        PeoplePanel.gameObject.SetActive(true);

        currentPanelTween?.Kill();
        currentPanelTween = ChatPanel.DOAnchorPosX(Panel_CloseRect.anchoredPosition.x, OpenCloseDuration)
            .SetEase(OpenCloseEase)
            .OnComplete(() => chatRoomPanel.SetActive(false));

        alreadyOpenChatRoom = false;
        currentChat = null;
        renderedIndicesForCurrentChat.Clear();
        RefreshchatPartnerList();
    }

    public void OpenChatRoom(Chat conversation)
    {
        if (conversation == null) return;
        StopMessageDisplay();
        currentChat = conversation;
        renderedIndicesForCurrentChat.Clear();

        chatRoomHeaderName.text = conversation.chatPartner.chatPartnerName;
        chatRoomHeaderImage.sprite = conversation.chatPartner.chatPartnerImage;
        chatMessageList.SetSender(conversation.chatPartner.chatPartnerName, conversation.chatPartner.chatPartnerImage);

        chatRoomPanel.SetActive(true);
        ChatPanel.gameObject.SetActive(true);
        currentPanelTween?.Kill();
        currentPanelTween = ChatPanel.DOAnchorPosX(Panel_OpenRect.anchoredPosition.x, OpenCloseDuration)
            .SetEase(OpenCloseEase)
            .OnComplete(() => chatPartnerListPanel.SetActive(false));

        alreadyOpenChatRoom = true;
        StartMessageDisplay(true);
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void RefreshchatPartnerList()
    {
        if (chatPartnerListContent == null || allChats == null) return;
        foreach (Transform child in chatPartnerListContent)
            Destroy(child.gameObject);

        List<Chat> chatsToShow = allChats
            .Where(chat => chat != null && chat.messages != null && GetArrivedMessages(chat).Count > 0)
            .OrderByDescending(chat =>
            {
                MessageReference last = GetArrivedMessages(chat).LastOrDefault();
                return last == null ? -1 : GetMessageDay(
                    chat.chatPartner.chatPartnerName,
                    last.index,
                    last.message.triggerId);
            })
            .ToList();

        foreach (Chat chat in chatsToShow)
        {
            UnreadInfo unreadInfo = GetUnreadInfo(chat);
            string preview = GetPreviewMessageText(chat, unreadInfo.hasUnread, out int count);
            GameObject item = Instantiate(chatPartnerListItemPrefab, chatPartnerListContent);
            ChatPartnerUI ui = item.GetComponent<ChatPartnerUI>();
            if (ui != null)
                ui.Setup(chat, preview, unreadInfo, this, count);
            else
                Debug.LogError($"Prefab '{chatPartnerListItemPrefab.name}' is missing ChatPartnerUI component.");
        }
    }

    private UnreadInfo GetUnreadInfo(Chat chat)
    {
        List<MessageReference> unread = GetArrivedMessages(chat)
            .Where(reference => !IsMessageRead(chat, reference.index))
            .ToList();

        return new UnreadInfo
        {
            hasUnread = unread.Count > 0,
            hasMandatory = unread.Any(reference => RequiresMandatoryPopup(chat, reference.index))
        };
    }

    private string GetPreviewMessageText(Chat chat, bool hasUnread, out int count)
    {
        List<MessageReference> arrived = GetArrivedMessages(chat);
        if (arrived.Count == 0)
        {
            count = 0;
            return "새로운 대화";
        }

        List<MessageReference> unread = arrived
            .Where(reference => !IsMessageRead(chat, reference.index))
            .ToList();
        count = unread.Count;
        return hasUnread && unread.Count > 0
            ? unread[0].message.messageText
            : arrived[arrived.Count - 1].message.messageText;
    }

    private void StartMessageDisplay(bool freshEntry)
    {
        if (messageDisplayCoroutine != null)
            StopCoroutine(messageDisplayCoroutine);
        messageDisplayCoroutine = StartCoroutine(ShowMessagesInOrderCoroutine(freshEntry));
    }

    private void StopMessageDisplay()
    {
        if (messageDisplayCoroutine != null)
            StopCoroutine(messageDisplayCoroutine);
        messageDisplayCoroutine = null;
        IsDisplayingMessages = false;
        messageRefreshPending = false;
        chatMessageList?.HideTypingMessage();
    }

    private IEnumerator ShowMessagesInOrderCoroutine(bool freshEntry)
    {
        IsDisplayingMessages = true;
        Chat displayingChat = currentChat;
        if (displayingChat == null)
        {
            IsDisplayingMessages = false;
            messageDisplayCoroutine = null;
            yield break;
        }

        if (freshEntry)
        {
            chatMessageList.ClearMessages();
            renderedIndicesForCurrentChat.Clear();
        }

        bool firstUnreadSkipped = false;
        bool separatorAdded = false;
        List<MessageReference> messages = GetArrivedMessages(displayingChat);
        foreach (MessageReference reference in messages)
        {
            if (currentChat != displayingChat) break;
            if (renderedIndicesForCurrentChat.Contains(reference.index)) continue;

            bool wasRead = IsMessageRead(displayingChat, reference.index);
            bool isPastDay = IsPastDayMessage(
                displayingChat.chatPartner.chatPartnerName,
                reference.index,
                reference.message.triggerId);
            bool animate = !isPastDay && (!freshEntry || !wasRead);
            if (!wasRead && !separatorAdded)
            {
                chatMessageList.AddNewChatSeparator();
                separatorAdded = true;
            }

            if (animate)
            {
                bool skipFirstToday = freshEntry && !firstUnreadSkipped
                    && !isPastDay;
                firstUnreadSkipped = true;
                if (!skipFirstToday)
                    yield return PlayTypingDelay(reference.message);
            }

            if (currentChat != displayingChat) break;
            int day = GetMessageDay(
                displayingChat.chatPartner.chatPartnerName,
                reference.index,
                reference.message.triggerId);
            CreateMessageBubble(day, reference.message.messageText, reference.message.triggerId);
            renderedIndicesForCurrentChat.Add(reference.index);

            if (!wasRead)
                MarkMessageRead(displayingChat, reference.index);

            ReportAlarmState();
            yield return null;
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        IsDisplayingMessages = false;
        messageDisplayCoroutine = null;

        if (messageRefreshPending && currentChat == displayingChat
            && chatRoomPanel != null && chatRoomPanel.activeInHierarchy)
        {
            messageRefreshPending = false;
            StartMessageDisplay(false);
        }
    }

    private IEnumerator PlayTypingDelay(ChatMessage message)
    {
        float total = Mathf.Max(message.delayAfterPrevious, message.messageText.Length / 10f);
        float preDelay = total / 3f;
        float typingDelay = total - preDelay;
        skipTyping = false;

        float elapsed = 0f;
        while (elapsed < preDelay && !skipTyping)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!skipTyping)
        {
            chatMessageList.ShowTypingMessage();
            elapsed = 0f;
            while (elapsed < typingDelay && !skipTyping)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        chatMessageList.HideTypingMessage();
        skipTyping = false;
    }

    private List<MessageReference> GetReferencesForTrigger(string triggerId)
    {
        List<MessageReference> result = new List<MessageReference>();
        if (allChats == null) return result;

        foreach (Chat chat in allChats)
        {
            if (chat == null || chat.chatPartner == null || chat.messages == null) continue;
            for (int i = 0; i < chat.messages.Count; i++)
            {
                if (chat.messages[i].triggerId != triggerId) continue;
                result.Add(new MessageReference { chat = chat, index = i, message = chat.messages[i] });
            }
        }
        return result;
    }

    private List<MessageReference> GetArrivedMessages(Chat chat)
    {
        List<MessageReference> result = new List<MessageReference>();
        if (chat == null || chat.messages == null) return result;

        foreach (string triggerId in progress.activatedTriggersOrdered)
        {
            for (int i = 0; i < chat.messages.Count; i++)
            {
                ChatMessage message = chat.messages[i];
                if (message.triggerId == triggerId && IsMessageRevealed(chat, i))
                    result.Add(new MessageReference { chat = chat, index = i, message = message });
            }
        }
        return result;
    }

    private void EnsureMessageState(string partnerName)
    {
        if (!progress.revealedMessageIndices.ContainsKey(partnerName))
            progress.revealedMessageIndices[partnerName] = new HashSet<int>();
        if (!progress.readMessageIndices.ContainsKey(partnerName))
            progress.readMessageIndices[partnerName] = new HashSet<int>();
        if (!progress.conversationSeenIndices.ContainsKey(partnerName))
            progress.conversationSeenIndices[partnerName] = -1;
    }

    private bool RevealMessage(MessageReference reference)
    {
        string partnerName = reference.chat.chatPartner.chatPartnerName;
        EnsureMessageState(partnerName);
        return progress.revealedMessageIndices[partnerName].Add(reference.index);
    }

    private bool IsMessageRevealed(Chat chat, int index)
    {
        if (chat == null || chat.chatPartner == null) return false;
        string partnerName = chat.chatPartner.chatPartnerName;
        return progress.revealedMessageIndices.TryGetValue(partnerName, out HashSet<int> indices)
            && indices.Contains(index);
    }

    private bool IsSlotMessageRead(Chat chat, int index)
    {
        if (chat == null || chat.chatPartner == null) return false;
        string partnerName = chat.chatPartner.chatPartnerName;
        return progress.readMessageIndices.TryGetValue(partnerName, out HashSet<int> indices)
            && indices.Contains(index);
    }

    private bool IsMessageRead(Chat chat, int index)
    {
        if (IsSlotMessageRead(chat, index)) return true;
        if (chat == null || chat.chatPartner == null || chat.useSaveSlotReadStateOnly) return false;
        return !MessengerSaveSystem.PlayAlarmForSeenMessages
            && MessengerSaveSystem.IsRead(chat.chatPartner.chatPartnerName, index);
    }

    private bool RequiresMandatoryPopup(Chat chat, int index)
    {
        if (chat == null || chat.messages == null || index < 0 || index >= chat.messages.Count) return false;
        if (!chat.messages[index].isMandatory || IsSlotMessageRead(chat, index)) return false;
        if (chat.useSaveSlotReadStateOnly) return true;
        return !MessengerSaveSystem.IsRead(chat.chatPartner.chatPartnerName, index);
    }

    private void MarkMessageRead(Chat chat, int index)
    {
        if (chat == null || chat.chatPartner == null || index < 0) return;
        string partnerName = chat.chatPartner.chatPartnerName;
        EnsureMessageState(partnerName);
        progress.readMessageIndices[partnerName].Add(index);

        if (!chat.useSaveSlotReadStateOnly)
            MessengerSaveSystem.MarkMessageAsRead(partnerName, index);

        UpdateContiguousSeenIndex(partnerName);
    }

    private void UpdateContiguousSeenIndex(string partnerName)
    {
        EnsureMessageState(partnerName);
        int contiguous = -1;
        while (progress.readMessageIndices[partnerName].Contains(contiguous + 1))
            contiguous++;
        progress.conversationSeenIndices[partnerName] = contiguous;
    }

    private void SetMessageDay(MessageReference reference, int day)
    {
        string partnerName = reference.chat.chatPartner.chatPartnerName;
        if (!progress.daySeparators.ContainsKey(partnerName))
            progress.daySeparators[partnerName] = new Dictionary<int, int>();
        if (!progress.daySeparators[partnerName].ContainsKey(reference.index))
            progress.daySeparators[partnerName][reference.index] = day;
    }

    private bool HasUnreadMessages(Chat chat)
    {
        return GetArrivedMessages(chat).Any(reference => !IsMessageRead(chat, reference.index));
    }

    private bool HasUnreadMessagesForAllChats()
    {
        return allChats != null && allChats.Any(HasUnreadMessages);
    }

    public void ReportAlarmState()
    {
        AlarmState state = mandatoryPopupOpen
            ? AlarmState.Mandatory
            : HasUnreadMessagesForAllChats() ? AlarmState.NonMandatory : AlarmState.None;
        PhoneManager.Instance?.UpdateAppAlarmState(
            AppKey.Messenger,
            state,
            playAlarmEffect: state != AlarmState.Mandatory,
            showAlarmUI: state != AlarmState.Mandatory);
    }

    private bool IsPastDayMessage(string partnerName, int originalIndex, string triggerId)
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.stage : 1;
        return GetMessageDay(partnerName, originalIndex, triggerId) < currentDay;
    }

    private int GetMessageDay(string partnerName, int originalIndex, string triggerId)
    {
        if (progress.daySeparators.TryGetValue(partnerName, out Dictionary<int, int> separators)
            && separators.TryGetValue(originalIndex, out int savedDay))
        {
            return savedDay;
        }

        if (int.TryParse(triggerId, out int day) && day != 0)
            return day;
        return GameManager.Instance != null ? GameManager.Instance.stage : 1;
    }

    private void CreateMessageBubble(int stage, string text, string triggerId)
    {
        int stageToPass = stage;
        if (PhoneManager.Instance != null && PhoneManager.Instance.isTutorial && !string.IsNullOrEmpty(triggerId))
            stageToPass = triggerId.GetHashCode();
        chatMessageList.AddMessage(stageToPass, text);
    }

    public MessengerProgress GetProgress() => progress;

    public void SetProgress(MessengerProgress loadedProgress)
    {
        progress = loadedProgress ?? new MessengerProgress();
        progress.conversationSeenIndices ??= new Dictionary<string, int>();
        progress.revealedMessageIndices ??= new Dictionary<string, HashSet<int>>();
        progress.readMessageIndices ??= new Dictionary<string, HashSet<int>>();
        progress.activatedTriggersOrdered ??= new List<string>();
        progress.daySeparators ??= new Dictionary<string, Dictionary<int, int>>();

        foreach (string partnerName in progress.readMessageIndices.Keys.ToList())
            UpdateContiguousSeenIndex(partnerName);

        RestorePendingMandatoryMessages();
        UpdateMessenger();
    }

    private void RestorePendingMandatoryMessages()
    {
        mandatoryEntries.Clear();
        heldOptionalMessages.Clear();
        mandatoryPopupOpen = false;
        mandatoryMessagePopup?.Hide();

        foreach (string triggerId in progress.activatedTriggersOrdered.ToList())
        {
            List<MessageReference> references = GetReferencesForTrigger(triggerId);
            List<MessageReference> pendingRequired = references
                .Where(reference => RequiresMandatoryPopup(reference.chat, reference.index))
                .ToList();

            if (pendingRequired.Count == 0)
            {
                foreach (MessageReference reference in references)
                {
                    if (!IsMessageRevealed(reference.chat, reference.index))
                        RevealMessage(reference);
                }
                continue;
            }

            foreach (MessageReference reference in references)
            {
                if (pendingRequired.Contains(reference))
                    mandatoryEntries.Add(new MandatoryEntry { reference = reference });
                else if (!IsMessageRevealed(reference.chat, reference.index))
                    heldOptionalMessages.Add(reference);
            }
        }

        if (mandatoryEntries.Count == 0) return;
        if (mandatoryMessagePopup == null)
        {
            Debug.LogError("[Messenger] 저장 복원 중 필수 메시지 팝업 참조를 찾지 못했습니다.", this);
            return;
        }

        mandatoryPopupOpen = true;
        mandatoryPopupIndex = 0;
        ShowCurrentMandatoryMessage();
    }

    public bool IsTriggerFullySeen(string triggerId)
    {
        foreach (MessageReference reference in GetReferencesForTrigger(triggerId))
        {
            if (!IsMessageRevealed(reference.chat, reference.index)
                || !IsMessageRead(reference.chat, reference.index))
            {
                return false;
            }
        }
        return true;
    }

    public void CheckCoroutineByTab(bool open)
    {
        if (!open && alreadyOpenChatRoom)
        {
            if (!closedByTabShowingChat)
                doubleClose = true;
            StopMessageDisplay();
            closedByTabShowingChat = true;
        }

        if (!closedByTabShowingChat || !open || !alreadyOpenChatRoom) return;
        if (doubleClose)
            doubleClose = false;
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
        OpenChatRoom(currentChat);
        closedByTabShowingChat = false;
    }
}
