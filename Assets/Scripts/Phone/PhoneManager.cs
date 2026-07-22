using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    Info,
    Tax,   // 국세청 앱 (5일마다 세금 납부). 맨 끝에 추가 — 기존 패널 인덱스 보존
}

public class PhoneManager : Singleton<PhoneManager>
{

    private Dictionary<AppKey, AlarmState> appAlarmStates = new Dictionary<AppKey, AlarmState>();
    private Dictionary<AppKey, bool> appAlarmPausing = new Dictionary<AppKey, bool>(); // 앱별: mandatory 알람이 게임을 멈출지 (기본 true)
    private bool anyPausingMandatory = false;
    public AlarmState TotalPhoneAlarmState { get; private set; } = AlarmState.None;

    [SerializeField] private GameObject mandatoryAlarm;
    [SerializeField] private GameObject nonMandatoryAlarm;

    // 저장 요소

    public bool PlayAlarmForSeenMessages => MessengerSaveSystem.PlayAlarmForSeenMessages;

    public void SetPlayAlarmForSeenMessages(bool val)
    {
        MessengerSaveSystem.PlayAlarmForSeenMessages = val;
    }

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
    [SerializeField] public TaxApp taxApp;

    //폰 페이즈 관련
    private float phoneTimer = 0;
    private bool skipPhoneTime = false;
    private bool isPhoneTime = false;

    [SerializeField] private GameObject skipPhoneTimeButton;
    [SerializeField] private BreedTimerManager breedTimerManager;
    [SerializeField] private TimerUI phoneTimerUI;
    [SerializeField] TextMeshProUGUI phoneTimerText;
    [SerializeField] public bool isTutorial = false;
    [SerializeField] public bool isTutorialEnd = false;


    [SerializeField] public GameObject weatherApp_Default;
    [SerializeField] public GameObject weatherApp_Tomorrow;

    private void Awake()
    {

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
        if (Input.GetKeyDown(toggleKey) && !isTutorial)
            Toggle();
    }



    public bool IsOpen => _isOpen;

    private float _empBlockEndTime = -1f; // 저주(통신장애): 이 시각(Time.time)까지 폰 차단

    /// <summary>저주(통신장애): 자유시간 시작 시 호출 — 낮 시간의 일부(EmpBlockRatio) 동안 폰을 차단.</summary>
    public void BeginEmpBlockIfActive(float freeTimeDuration)
    {
        _empBlockEndTime = CurseState.EmpBlockRatio > 0f
            ? Time.time + freeTimeDuration * CurseState.EmpBlockRatio
            : -1f;
    }

    public void Toggle()
    {
        // 저주(통신장애): 낮 시간의 앞부분 동안 폰이 안 열림(사용자 조작만 차단; 강제 개방/닫기는 영향 없음)
        if (!_isOpen && CurseState.EmpBlockRatio > 0f && Time.time < _empBlockEndTime)
        {
            PhoneTouchEffect(); // TODO: 치지직 노이즈 표시
            return;
        }
        SetOpen(!_isOpen);
    }

    public void SetOpen(bool open)
    {
        PhoneTouchEffect();
        if (isTutorial && isTutorialEnd)
            return;
        _isOpen = open;
        if (phoneRoot != null) phoneRoot.SetActive(open);
        phoneBtn.SetActive(!open);
        messengerApp.CheckCoroutineByTab(open);
        if (open)
        {
            alarm.DisableAlarm();
        }
        else
        {
            // 핸드폰을 닫을 때 notification이 표시 중이면 먼저 닫기 (알람과의 충돌 방지)
            PhoneNotificationBus.OnHide?.Invoke();
            // 핸드폰을 닫을 때 알람 UI 닫기
            mandatoryAlarm.SetActive(false);
            nonMandatoryAlarm.SetActive(false);
            alarm.EnableAlarm();
        }
        //FindAnyObjectByType<UIAnimationManager>().SwitchFollowTarget();
        //if (open) RefreshTopBar();
    }

    public void OpenHome()
    {
        _current = null;
        PhoneTouchEffect();
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
        PhoneTouchEffect();
        transitionController.TransitionToIndex((int)key + 1);
        _current = key;

        if (key == AppKey.Tax && taxApp != null)
            taxApp.Refresh(); // 국세청 앱 열 때 이번 세금액/마감 갱신
    }
    public void PhoneTouchEffect()
    {
        SoundManager.Instance.PlayEffect("PhoneTouch");
    }

    // 이 밤에 "처음 등장"하는 웨이브가 있으면 해당 경고 메시지(명명 트리거)를 발송한다.
    // 트리거 발송 시점이 WaveSchedule.GetFirstAppearStage에서 파생되므로, 밸런스 변경 시 메시지도 자동으로 따라온다.
    private void FireWaveUnlockTriggers(int stage)
    {
        foreach (WaveType type in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (type == WaveType.None || type == WaveType.Aging) continue;
            if (stage != WaveSchedule.GetFirstAppearStage(type)) continue;

            string trigger = WaveSchedule.GetUnlockTriggerId(type);
            if (!string.IsNullOrEmpty(trigger))
                messengerApp.ActivateTrigger(trigger);
        }
    }

    // 벌레 엔티티 진행(첫 등장 + 종류 증가)에 묶인 경고 메시지를 발송한다.
    // 발송 시점은 BugSchedule에서 파생되므로(벌레 등장 스테이지/주기 변경 시 자동으로 따라옴),
    // 여기서는 BugSchedule이 알려주는 트리거를 그대로 쏘기만 한다.
    private void FireBugTriggers(int stage)
    {
        string trigger = BugSchedule.GetMessageTrigger(stage);
        if (!string.IsNullOrEmpty(trigger))
            messengerApp.ActivateTrigger(trigger); // 해당 단계 메시지가 없으면 no-op
    }

    // 5n일(세금일)인데 세금이 미납 상태인가. (TaxManager 없으면 false → 안전)
    private bool IsTaxUnpaidNight()
    {
        int stage = GameManager.Instance.stage;
        return TaxSchedule.IsTaxStage(stage)
            && TaxManager.Instance != null
            && !TaxManager.Instance.IsPaidForStage(stage);
    }

    public IEnumerator PhonePhase()
    {
        ClickRouter.Instance.IsBlockedByUI = true;
        SetPhoneTimer();
        messengerApp.ActivateTrigger(GameManager.Instance.stage.ToString()); // 숫자 트리거(플레이버 메시지)
        FireWaveUnlockTriggers(GameManager.Instance.stage);                   // 날씨 웨이브 경고(명명 트리거)
        FireBugTriggers(GameManager.Instance.stage);                          // 벌레 등장/종류 경고(명명 트리거)

        skipPhoneTimeButton.SetActive(true);
        phoneTimer = GameManager.Instance.grid.GetMaxBreedTimer();
        phoneTimerUI.StartPhoneTimer();

        // 세금 미납 경고: 국세청(Tax) 앱 아이콘에 빨간 red dot(Mandatory)을 띄우되,
        // pauseIfMandatory:false 로 게임은 멈추지 않게 한다(밤 타이머는 계속 흘러 실패 판정 가능).
        // 납부 시 TaxApp에서 None으로 해제.
        if (IsTaxUnpaidNight())
            UpdateAppAlarmState(AppKey.Tax, AlarmState.Mandatory, pauseIfMandatory: false);

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
                        title = "내일이 얼마 남지 않았습니다",
                        message = "또 힘내봅시다.",
                        duration = 5f
                    });
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

        // 미납이어도 밤은 그대로 끝난다. 소비(강제징수/압류)는 다음 낮 시작에 처리(GameManager.TaxCollectionRoutine).
        yield return null;
    }

    public void SetPhoneTimer()
    {
        breedTimerManager.SetPhoneTimer();
        phoneTimerText.text = "자기 전에\n핸드폰 봐야지...";
    }

    // 세금 압류 유예 타이머(GameManager.TaxCollectionRoutine에서 사용)
    public void StartTaxTimer(int seconds) => breedTimerManager.StartTaxTimer(seconds);
    public void StopTaxTimer() => breedTimerManager.StopTaxTimer();

    public void SetPhoneTimerUI(TimerUI timerUI)
    {
        phoneTimerUI = timerUI;
    }

    public float GetMaxPhoneTimer()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.grid.GetMaxBreedTimer();
        return 30f;
    }

    public void SkipPhoneTime()
    {
        if (GameManager.Instance.GetGameIsStopped())
            return;
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
    // pauseIfMandatory: 이 알람이 Mandatory일 때 게임을 멈출지(기본 true). 세금 같은 "빨간 알림이지만 멈추지 않는" 용도로 false.
    public void UpdateAppAlarmState(AppKey appKey, AlarmState newState, bool pauseIfMandatory = true)
    {
        appAlarmStates[appKey] = newState;
        appAlarmPausing[appKey] = pauseIfMandatory;
        RefreshTotalAlarmState();
        UpdateAppIconUI(appKey, newState);
    }

    private void RefreshTotalAlarmState()
    {
        AlarmState highestState = AlarmState.None;
        bool pausing = false;

        foreach (var kv in appAlarmStates)
        {
            if (kv.Value == AlarmState.Mandatory)
            {
                highestState = AlarmState.Mandatory;
                // 멈춤을 요청한 mandatory가 하나라도 있으면 폰 전체가 멈춤(기본 true)
                if (!appAlarmPausing.TryGetValue(kv.Key, out bool p) || p)
                    pausing = true;
            }
            else if (kv.Value == AlarmState.NonMandatory && highestState != AlarmState.Mandatory)
            {
                highestState = AlarmState.NonMandatory;
            }
        }

        anyPausingMandatory = pausing;
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
                alarm.AlarmPermanent();
                if (GameManager.Instance != null)
                {
                    if (anyPausingMandatory) // 멈춤을 요청한 mandatory만 게임 정지
                    {
                        GameManager.Instance.StopGame();
                        GameManager.Instance.grid.GetBreedTimerUI().ShowPhoneAlarmText();
                    }
                    else
                    {
                        GameManager.Instance.ResumeGame();
                        GameManager.Instance.grid.GetBreedTimerUI().HidePhoneAlarmText();
                    }
                }
                break;
            case AlarmState.NonMandatory:
                mandatoryAlarm.SetActive(false);
                nonMandatoryAlarm.SetActive(true);
                alarm.AlarmImpermanent();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResumeGame();
                    GameManager.Instance.grid.GetBreedTimerUI().HidePhoneAlarmText();
                }
                break;
            case AlarmState.None:
                mandatoryAlarm.SetActive(false);
                nonMandatoryAlarm.SetActive(false);
                alarm.StopAlarm();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ResumeGame();
                    GameManager.Instance.grid.GetBreedTimerUI().HidePhoneAlarmText();
                }
                break;
        }
    }

    private void UpdateAppIconUI(AppKey key, AlarmState state)
    {
        int i = (int)key;
        if (mandatoryAppAlarm == null || nonMandatoryAppAlarm == null
            || i < 0 || i >= mandatoryAppAlarm.Count || i >= nonMandatoryAppAlarm.Count)
            return; // 해당 앱의 아이콘 알람 UI 미등록(예: Tax 앱 아이콘 미배선) — 크래시 방지

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
        phoneTimer = GetMaxPhoneTimer();
        phoneTimerUI.StartPhoneTimer();

        isPhoneTime = true;
    }

    public bool GetIsPhoneTime()
    {
        return isPhoneTime;
    }

    public void SetWeatherForecastPanel()
    {
        if (!GameManager.Instance.grid.GetHasWeatherForecast())
        {
            weatherApp_Default.SetActive(true);
            weatherApp_Tomorrow.SetActive(false);
            GameManager.Instance.enemyController.SetWeatherApp(weatherApp_Default.GetComponent<WeatherApp>());
        }
        else
        {
            weatherApp_Default.SetActive(false);
            weatherApp_Tomorrow.SetActive(true);
            GameManager.Instance.enemyController.SetWeatherApp(weatherApp_Tomorrow.GetComponent<WeatherApp>());
        }
    }

}


