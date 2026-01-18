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

    protected void CompleteOnce()
    {
        if (IsCompleted) return;
        State = RequestState.Complete;
        GrantRewardOnce();
        RaiseChanged();
    }

    protected void RaiseChanged() => OnChanged?.Invoke(this);

    public virtual void GrantRewardOnce()
    {
        if (!IsCompleted) return;
        if (rewardGranted) return;

        State = RequestState.Granted;

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