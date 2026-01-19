using System;
using UnityEngine;

public enum RequestState
{
    InProgress, // 진행 중
    Complete, // 완료(보상 미지급)
    Granted, // 보상 지급
    Fail // 실패
}

public abstract class RequestInstance
{
    public RequestScriptable Data { get; }
    public RequestState State { get; protected set; } = RequestState.InProgress;

    public bool IsCompleted => State == RequestState.Complete || State == RequestState.Granted;
    public bool rewardGranted => State == RequestState.Granted;
    public bool CanAcceptReward => State == RequestState.Complete;
    public bool IsFailed => State == RequestState.Fail;

    public event Action<RequestInstance> OnChanged;

    protected RequestInstance(RequestScriptable data)
    {
        Data = data;
    }

    public virtual void Start()
    {
        State = RequestState.InProgress;
        RaiseChanged();
    }

    public virtual void Stop() { }

    public abstract string GetProgressText();

    public virtual string GetTitleText()
    {
        return Data.requestTitle;
    }

    protected void CompleteOnce()
    {
        if (IsCompleted) return;
        State = RequestState.Complete;
        //완료 알람을 보내야 함(상단바)
        PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = "완료된 퀘스트가 있습니다",
                        message = "수령 버튼을 눌러 보상을 획득해 주세요.",
                        duration = 3f
                    }
                );
        //완료 알람(앱 아이콘)
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.NonMandatory);
        RaiseChanged();
    }

    protected void RaiseChanged() => OnChanged?.Invoke(this);

    public virtual void GrantRewardOnce()
    {
        if (!IsCompleted) return;
        if (rewardGranted) return;

        State = RequestState.Granted;
        RaiseChanged();
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.None);
        //GameManager.Instance?.questToken += Data.rewardTokens;
        Debug.Log("보상 획득 완료");
    }

    public void MarkFailed()
    {
        if (State == RequestState.Granted) return;

        State = RequestState.Fail;
        RaiseChanged();
    }

    public abstract RequestInstanceSaveData ToSaveData();
    public abstract void LoadFromSaveData(RequestInstanceSaveData data);
}
