using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[System.Serializable]
public class RequestInstanceSaveData
{
    public string requestId;
    public string typeCode;
    public int progressCount; // ??? ??
    public int state;
    public List<string> extraStrings; //buymerch
    public int extraInt; //sellspecificpea & peasurvive
}

public class RequestManager : Singleton<RequestManager>
{
    public int cycle = 5;
    public int requestNum = 3;
    public int DayPassed => Board.DayPassed;

    [SerializeField] private TextMeshProUGUI appTitle;

    private bool newRequestArrived = false;

    [SerializeField] private List<RequestScriptable> requestPool = new();

    /// <summary>의뢰 뽑기와 진행. 이 컴포넌트는 알림과 저장, 화면 갱신을 더한다.</summary>
    public RequestBoard Board
    {
        get
        {
            if (board != null) return board;
            board = new RequestBoard(requestPool, cycle, requestNum, new GameRequestWorld());
            board.Changed += _ => OnProgressUpdated?.Invoke();
            board.Completed += _ => PushRequestCompleteAlarm();
            return board;
        }
    }
    private RequestBoard board;

    public IReadOnlyList<RequestInstance> ActiveReq => Board.Active;
    public int CycleEndRound => Board.CycleEndRound;

    public event Action OnBoardUpdated;
    public event Action OnProgressUpdated;

    public int CompleteRequestCount => Board.CompletedCount;

    private void OnEnable()
    {
        Loc.LanguageChanged += UpdateRequestAppTitle;
        GameEvents.OnQuestDayPassed += HandleDayPassed;
        ((GameRequestWorld)Board.World).Connect();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= UpdateRequestAppTitle;
        GameEvents.OnQuestDayPassed -= HandleDayPassed;
        ((GameRequestWorld)Board.World).Disconnect();
        Board.ClearActive();
    }

    private void Start()
    {

    }

    private void HandleDayPassed()
    {
        Board.DayPassed++;
        UpdateRequestAppTitle();
    }

    public void StartNewCycle(int startRound)
    {
        if (GameManager.Instance?.Tutorial?.CanUseQuests == false) return;

        ClearRequestAlarm();
        if (Board.StartCycle(startRound, GameRun.Current.Shop.UniqueSeed + startRound * 100))
            PushRequestGenerateAlarm();
        UpdateRequestAppTitle();

        OnBoardUpdated?.Invoke();
    }

    /// <summary>진행 중인 의뢰를 저장 데이터에 담는다. <see cref="LoadRequestManager"/>와 짝.</summary>
    public void SaveRequestManager(RequestSave save) => Board.Save(save);

    public void LoadRequestManager(RequestSave saveData)
    {
        Board.Load(saveData);
        OnBoardUpdated?.Invoke();
    }

    private void ClearRequestAlarm()
    {
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.None);
    }

    private void PushRequestGenerateAlarm()
    {
        PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = Loc.T("game.quest.new_title"),
                        message = Loc.T("game.quest.new_message"),
                        duration = 5f
                    }
                );

        if (PhoneManager.Instance.CurrentApp == AppKey.Quest)
        {
            newRequestArrived = false;
        }
        else
        {
            newRequestArrived = true;
            PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.NonMandatory);
        }
    }

    private static void PushRequestCompleteAlarm()
    {
        //완료 알람을 보내야 함(상단바)
        PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = Loc.T("game.quest.complete_title"),
                        message = Loc.T("game.quest.complete_message"),
                        duration = 3f
                    }
                );

        //완료 알람(앱 아이콘)
        PhoneManager.Instance?.UpdateAppAlarmState(AppKey.Quest, AlarmState.NonMandatory);
    }

    public void OnCheckedNewRequest()
    {
        if (newRequestArrived)
        {
            newRequestArrived = false;
            PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.None);
        }
    }

    public void UpdateRequestAppTitle()
    {
        if (CycleEndRound < 0 || appTitle == null) return;

        appTitle.text = Loc.T("game.quest.weekly_title", ("days", cycle - DayPassed));
    }
}
