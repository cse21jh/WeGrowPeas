using System;
using UnityEngine;

public abstract class RequestInstance
{
    public RequestScriptable Data { get; }

    public bool IsCompleted { get; protected set; }

    public event Action<RequestInstance> OnChanged;

    protected bool rewardGranted;

    protected RequestInstance(RequestScriptable data)
    {
        Data = data;
    }

    public virtual void Start()
    {
        IsCompleted = false;
        rewardGranted = false;
        RaiseChanged();
    }

    public virtual void Stop() { }

    public abstract string GetProgressText();

    protected void CompleteOnce()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        GrantRewardOnce();
        RaiseChanged();
    }

    protected void RaiseChanged() => OnChanged?.Invoke(this);

    public virtual void GrantRewardOnce()
    {
        if (!IsCompleted) return;
        if (rewardGranted) return;

        rewardGranted = true;

        //GameManager.Instance?.questToken += Data.rewardTokens;
        Debug.Log("º¸»ó È¹µæ ¿Ï·á");
    }

    public abstract RequestInstanceSaveData ToSaveData();
    public abstract void LoadFromSaveData(RequestInstanceSaveData data);
}