using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

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


    //폰 페이즈 관련
    private float maxPhoneTimer = 30.0f;
    private float phoneTimer = 0;
    private bool skipPhoneTime = false;
    
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
        ClickRouter.Instance.IsBlockedByUI = true;
        SetPhoneTimer();
        
        skipPhoneTimeButton.SetActive(true);
        phoneTimer = maxPhoneTimer;
        phoneTimerUI.StartPhoneTimer();

        bool _warned15s = false;
        //int rerollCount = 0;

        while (!skipPhoneTime && (phoneTimer > 0))
        {
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
}


public enum AppKey
{
    Weather,
    Shop,
    Quest,
    News,
}