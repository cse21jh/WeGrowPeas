using System;
using UnityEngine;

public abstract class RequestInstance
{
    public RequestScriptable Data { get; }
    public bool IsCompleted { get; protected set; }

    public event Action<RequestInstance> OnChanged;

    protected RequestInstance(RequestScriptable data)
    {
        Data = data;
    }

    public virtual void Start()
    {
        IsCompleted = false;
        RaiseChanged();
    }

    public virtual void Stop() { }

    public abstract string GetProgressText();

    protected void CompleteOnce()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        RaiseChanged();
    }

    protected void RaiseChanged() => OnChanged?.Invoke(this);

    public virtual void GrantRewardOnce()
    {
        if (!IsCompleted) return;

        //GameManager.Instance?.questToken += Data.rewardTokens;
    }



}