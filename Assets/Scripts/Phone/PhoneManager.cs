using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public enum AlarmState
{
    None = 0,        // 알람 없음
    NonMandatory = 1, // 읽지 않아도 되는 알람 (선택)
    Mandatory = 2     // 반드시 읽어야 하는 알람 (필수)
}
public enum AppKey
{
    Weather,
    Shop,
    Quest,
    Messenger,
}

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

    private Dictionary<AppKey, AlarmState> appAlarmStates = new Dictionary<AppKey, AlarmState>();
    public AlarmState TotalPhoneAlarmState { get; private set; } = AlarmState.None;

    [SerializeField] private GameObject mandatoryAlarm;
    [SerializeField] private GameObject nonMandatoryAlarm;

    [SerializeField] private List<GameObject> mandatoryAppAlarm;
    [SerializeField] private List<GameObject> nonMandatoryAppAlarm;

    PhoneAlarmEffectController alarm;

    [Serializable]
    public class AppEntry
    {
        public AppKey key;
        public string displayName;   // (선택) 상단 타이틀용
        public GameObject prefab;    // 앱 프리팹
    }

    //[Header("Apps (prefab, created once at startup)")]
    //[SerializeField] private List<AppEntry> apps = new();

    [Header("UI Roots")]
    [SerializeField] private GameObject phoneRoot;   // 폰 전체 루트 (열고/닫기)
    [SerializeField] private GameObject phoneBtn;    // 폰 열기 버튼
    [SerializeField] private GameObject homePanel;   // 홈 패널(고정 UI)
    [SerializeField] private Transform appContainer; // 앱 인스턴스 부모(빈 RectTransform)
    [SerializeField] private PhoneTopBar topBar;     // (선택) 제목/뒤로/홈

    [SerializeField] private PanelTranstionController transitionController;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    //private readonly Dictionary<AppKey, GameObject> _instances = new();
    private AppKey? _current = null;
    private bool _isOpen;


    public MessengerApp messengerApp;

    //폰 페이즈 관련
    private float maxPhoneTimer = 30.0f;
    private float phoneTimer = 0;
    private bool skipPhoneTime = false;
    private bool isPhoneTime = false;
    
    [SerializeField] private GameObject skipPhoneTimeButton;
    [SerializeField] private BreedTimerManager breedTimerManager;
    [SerializeField] private TimerUI phoneTimerUI;
    [SerializeField] TextMeshProUGUI phoneTimerText;    


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (phoneRoot != null) phoneRoot.SetActive(false);
        _isOpen = false;

        foreach (AppKey key in Enum.GetValues(typeof(AppKey)))
            appAlarmStates[key] = AlarmState.None;

        alarm = GetComponent<PhoneAlarmEffectController>();
    }

    private void Start()
    {
        //CreateAllAppsOnce();

        //if (homePanel != null) homePanel.SetActive(true);

        //RefreshTopBar();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle() => SetOpen(!_isOpen);

    public void SetOpen(bool open)
    {   
        _isOpen = open;
        if (phoneRoot != null) phoneRoot.SetActive(open);
        phoneBtn.SetActive(!open);
        messengerApp.CheckCoroutineByTab(open);

        FindAnyObjectByType<UIAnimationManager>().SwitchFollowTarget();
        //if (open) RefreshTopBar();
    }

    public void OpenHome()
    {
        _current = null;

        transitionController.TransitionToIndex(0);
        //RefreshTopBar();
    }

    public void OpenApp(AppKey key)
    {
        /*
        EnsureOpen();

        if (!_instances.TryGetValue(key, out var go) || go == null)
        {
            Debug.LogWarning($"[Phone] App instance not found: {key}");
            return;
        }

        if (homePanel != null) homePanel.SetActive(false);

        HideCurrentApp();

        go.SetActive(true);
        _current = key;
        */

        //RefreshTopBar();

        transitionController.TransitionToIndex((int)key + 1);
        _current = key;
    }

    public IEnumerator PhonePhase()
    {
        messengerApp.ActivateTrigger(GameManager.Instance.stage.ToString());
        ClickRouter.Instance.IsBlockedByUI = true;
        SetPhoneTimer();
        
        skipPhoneTimeButton.SetActive(true);
        phoneTimer = maxPhoneTimer;
        phoneTimerUI.StartPhoneTimer();

        bool _warned15s = false;
        //int rerollCount = 0;
        isPhoneTime = true;
        while (!skipPhoneTime && (phoneTimer > 0))
        {
            if (GameManager.Instance.GetGameIsStopped())
            {
                yield return null;
                continue;
            }

            phoneTimer -= Time.deltaTime;

            if (phoneTimer < 15f && !_warned15s)
            {
                _warned15s = true;

                PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = "내일이 얼마남지 않았습니다",
                        message = "또 힘내봅시다.",
                        duration = 5f
                    }
                );
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SkipPhoneTime();
            }

            yield return null;
        }
        phoneTimerUI.StopTimerByPhone();
        skipPhoneTimeButton.SetActive(false);
        ClickRouter.Instance.IsBlockedByUI = false;
        skipPhoneTime = false;
        isPhoneTime = false;
        yield return null;
    }
    
    public void SetPhoneTimer()
    {
        breedTimerManager.SetPhoneTimer();
        phoneTimerText.text = "자기 전에 핸드폰 봐야지...";
    }

    public void SetPhoneTimerUI(TimerUI timerUI)
    {
        phoneTimerUI = timerUI;
    }

    public float GetMaxPhoneTimer()
    {
        return maxPhoneTimer;
    }

    public void SkipPhoneTime()
    {
        skipPhoneTime = true;
    }

    /*

    public void HandleBack()
    {
        // 앱이면 홈으로, 홈이면 폰 닫기
        if (_current.HasValue) OpenHome();
        else SetOpen(false);
    }

    private void EnsureOpen()
    {
        if (!_isOpen) SetOpen(true);
    }

    private void HideCurrentApp()
    {
        if (!_current.HasValue) return;

        var key = _current.Value;
        if (_instances.TryGetValue(key, out var go) && go != null)
            go.SetActive(false);
    }

    private void CreateAllAppsOnce()
    {
        if (appContainer == null)
        {
            Debug.LogError("[Phone] appContainer is not assigned.");
            return;
        }

        _instances.Clear();

        for (int i = 0; i < apps.Count; i++)
        {
            var e = apps[i];
            if (e == null) continue;

            if (e.prefab == null)
            {
                Debug.LogWarning($"[Phone] Missing prefab for {e.key}");
                continue;
            }

            if (_instances.ContainsKey(e.key))
            {
                Debug.LogWarning($"[Phone] Duplicate AppKey: {e.key}");
                continue;
            }

            var go = Instantiate(e.prefab, appContainer);
            go.name = $"App_{e.key}";
            go.SetActive(false);

            _instances.Add(e.key, go);
        }
    }

    private void RefreshTopBar()
    {
        if (topBar == null) return;

        if (!_current.HasValue)
        {
            topBar.SetTitle("핸드폰");
            return;
        }

        // 타이틀: 엔트리 displayName 우선, 없으면 enum 이름
        var key = _current.Value;
        string title = null;

        for (int i = 0; i < apps.Count; i++)
        {
            var e = apps[i];
            if (e != null && e.key == key)
            {
                title = e.displayName;
                break;
            }
        }

        if (string.IsNullOrEmpty(title)) title = key.ToString();

        topBar.SetTitle(title);
    }
    */
    public void UpdateAppAlarmState(AppKey appKey, AlarmState newState)
    {
        appAlarmStates[appKey] = newState;
        RefreshTotalAlarmState();
        UpdateAppIconUI(appKey, newState);
    }

    private void RefreshTotalAlarmState()
    {
        AlarmState highestState = AlarmState.None;

        foreach (var state in appAlarmStates.Values)
        {
            if (state == AlarmState.Mandatory)
            {
                highestState = AlarmState.Mandatory;                
                break; // 하나라도 필수면 즉시 최상위 등급
            }
            if (state == AlarmState.NonMandatory)
            {
                highestState = AlarmState.NonMandatory;                
            }
        }
        if(TotalPhoneAlarmState == AlarmState.Mandatory &&  highestState != AlarmState.Mandatory)
        {
            
        }

        TotalPhoneAlarmState = highestState;
        ApplyPhoneAlarmUI();
    }

    private void ApplyPhoneAlarmUI()
    {
        // 폰 외부 버튼이나 전체 루트 UI에 알람 수위 적용
        switch (TotalPhoneAlarmState)
        {
            case AlarmState.Mandatory:
                mandatoryAlarm.SetActive(true);
                nonMandatoryAlarm.SetActive(false);
                Debug.Log("<color=red>폰 전체: 필수 알람 울림!</color>");
                alarm.AlarmPermanent();
                if(GameManager.Instance != null)
                    GameManager.Instance.StopGame();
                break;
            case AlarmState.NonMandatory:
                mandatoryAlarm.SetActive(false);
                nonMandatoryAlarm.SetActive(true);
                Debug.Log("<color=yellow>폰 전체: 선택 알람 있음</color>");
                alarm.AlarmImpermanent();
                if (GameManager.Instance != null)
                    GameManager.Instance.ResumeGame();               
                break;
            case AlarmState.None:
                mandatoryAlarm.SetActive(false);
                nonMandatoryAlarm.SetActive(false);
                alarm.StopAlarm();
                if (GameManager.Instance != null)
                    GameManager.Instance.ResumeGame();                
                Debug.Log("폰 전체: 알람 없음");
                break;
        }
    }

    private void UpdateAppIconUI(AppKey key, AlarmState state)
    {
        switch (state)
        {
            case AlarmState.Mandatory:
                nonMandatoryAppAlarm[(int)key].SetActive(false);
                mandatoryAppAlarm[(int)key].SetActive(true);
                break;
            case AlarmState.NonMandatory:
                nonMandatoryAppAlarm[(int)key].SetActive(true);
                mandatoryAppAlarm[(int)key].SetActive(false);
                break;
            case AlarmState.None:
                nonMandatoryAppAlarm[(int)key].SetActive(false);
                mandatoryAppAlarm[(int)key].SetActive(false);
                break;
        }
    }

    public void LoadPhoneManager(SaveData saveData)
    {
        MessengerProgress progress = new MessengerProgress();

        for (int i = 0; i < saveData.chatPartners.Count; i++)
        {
            progress.conversationSeenIndices.Add(saveData.chatPartners[i], saveData.conversationSeenIndices[i]);
        }
        for (int i = 0; i < saveData.dayChatPartners.Count; i++)
        {
            string partnerName = saveData.dayChatPartners[i];

            ChatDayData chatDayData = saveData.dayByChatPartners[i];
            

            Dictionary<int, int> separatorsForPartner = new Dictionary<int, int>();
            for (int j = 0; j < chatDayData.index.Count; j++)
            {
                int messageIndex = chatDayData.index[j];
                int day = chatDayData.day[j];

                separatorsForPartner.Add(messageIndex, day);
            }
            progress.daySeparators.Add(partnerName, separatorsForPartner);
        }
        foreach (var r in saveData.activatedTriggers)
        {
            progress.activatedTriggersOrdered.Add(r);
        }
        messengerApp.SetProgress(progress);
    }
    public void TutorialPhonePhase()
    {
        ClickRouter.Instance.IsBlockedByUI = true;
        SetPhoneTimer();

        skipPhoneTimeButton.SetActive(true);
        phoneTimer = maxPhoneTimer;
        phoneTimerUI.StartPhoneTimer();

        isPhoneTime = true;
    }

    public bool GetIsPhoneTime()
    {
        return isPhoneTime;
    }
}


