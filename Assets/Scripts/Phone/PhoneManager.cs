using System;
using System.Collections.Generic;
using UnityEngine;

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

    [Serializable]
    public class AppEntry
    {
        public string displayName;
        public BasePhoneApp appRoot;
    }

    [Header("Apps (Fixed in Scene)")]
    [SerializeField] private List<AppEntry> apps = new();

    [Header("UI Roots")]
    [SerializeField] private GameObject phoneRoot;   // 폰 전체 루트
    [SerializeField] private GameObject homePanel;   // 홈 패널(스크립트 없음)
    [SerializeField] private PhoneTopBar topBar;     // 상단바

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private BasePhoneApp _currentApp;
    private bool _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (phoneRoot != null) phoneRoot.SetActive(false);
        _isOpen = false;
    }

    private void Start()
    {
        // 앱 1회 초기화 + 비활성
        for (int i = 0; i < apps.Count; i++)
        {
            var e = apps[i];
            if (e == null || e.appRoot == null) continue;

            e.appRoot.gameObject.SetActive(false);
            e.appRoot.OnCreate(this);
        }

        // 홈도 시작은 켜둔 상태로 준비 (폰이 닫혀있으면 안 보임)
        if (homePanel != null) homePanel.SetActive(true);

        // 논리 상태는 Home으로
        _currentApp = null;
        RefreshTopBar();
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
        if (open) RefreshTopBar();
    }

    public void OpenHome()
    {
        EnsureOpen();
        HideCurrentApp();

        if (homePanel != null) homePanel.SetActive(true);

        RefreshTopBar();
    }

    public void OpenApp(BasePhoneApp app)
    {
        if (app == null) return;

        EnsureOpen();

        if (homePanel != null) homePanel.SetActive(false);

        if (_currentApp != null && _currentApp != app)
        {
            _currentApp.OnHide();
            _currentApp.gameObject.SetActive(false);
        }

        _currentApp = app;
        _currentApp.gameObject.SetActive(true);
        _currentApp.OnShow();

        RefreshTopBar();
    }

    public void HandleBack()
    {
        // 앱이면 홈으로, 홈이면 폰 닫기
        if (_currentApp != null) OpenHome();
        else SetOpen(false);
    }

    private void EnsureOpen()
    {
        if (!_isOpen) SetOpen(true);
    }

    private void HideCurrentApp()
    {
        if (_currentApp == null) return;

        _currentApp.OnHide();
        _currentApp.gameObject.SetActive(false);
        _currentApp = null;
    }

    private void RefreshTopBar()
    {
        if (topBar == null) return;

        if (_currentApp == null)
        {
            topBar.SetTitle("핸드폰");
        }
        else
        {
            string title = _currentApp.Title;
            if (string.IsNullOrEmpty(title))
            {
                var entry = FindEntry(_currentApp);
                title = entry != null ? entry.displayName : "앱";
            }

            topBar.SetTitle(title);
        }
    }

    private AppEntry FindEntry(BasePhoneApp app)
    {
        for (int i = 0; i < apps.Count; i++)
        {
            var e = apps[i];
            if (e != null && e.appRoot == app)
                return e;
        }
        return null;
    }
}
