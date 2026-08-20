using System;
using System.Collections.Generic;
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

    public virtual string GetDescriptionText()
    {
        return Data.requestDescription;
    }

    public string GetRewardText()
    {
        List<string> rewardStrings = new List<string>();

        foreach (var r in Data.rewards)
        {
            switch (r.type)
            {
                case RewardType.Gold:
                    rewardStrings.Add(r.amount.ToString() + "G");
                    break;
                case RewardType.Gene:
                    rewardStrings.Add(r.amount.ToString() + "유전자");
                    break;
            }
        }

        return string.Join(" + ", rewardStrings);
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
        PhoneManager.Instance?.UpdateAppAlarmState(AppKey.Quest, AlarmState.NonMandatory);
        RaiseChanged();
    }

    protected void RaiseChanged() => OnChanged?.Invoke(this);

    /// <summary>이것 말고도 아직 수령하지 않은 보상이 남아 있는가.</summary>
    private bool HasOtherClaimableReward()
    {
        var manager = RequestManager.Instance;
        if (manager == null || manager.ActiveReq == null) return false;

        foreach (var request in manager.ActiveReq)
            if (request != null && request != this && request.CanAcceptReward)
                return true;

        return false;
    }

    public virtual void GrantRewardOnce()
    {
        if (!IsCompleted)
        {
            SoundManager.Instance?.PlayEffect("WrongSelect");
            return;
        }
        if (rewardGranted) return;

        // 아래 매니저들은 씬 구성에 따라 없을 수 있다(농장 씬만 열고 테스트하는 경우 등).
        // 하나가 없다고 보상 지급 자체가 중간에 끊기면 안 되므로 전부 널 검사한다.
        foreach (var r in Data.rewards)
        {
            switch (r.type)
            {
                case RewardType.Gold:
                    GameManager.Instance?.economyManager?.AddGold(r.amount);
                    break;
                case RewardType.Gene:
                    //추가
                    break;
            }
        }

        switch (Data.requestDifficulty)
        {
            case RequestDifficulty.Easy:
                break;
            case RequestDifficulty.Normal:
                AbilityManager.Instance?.AddGeneStorage(5);
                break;
            case RequestDifficulty.Hard:
                AbilityManager.Instance?.AddGeneStorage(10);
                break;
        }

        //customize token 추가

        State = RequestState.Granted;

        RaiseChanged();

        // 아직 받지 않은 보상이 남아 있으면 알람을 끄지 않는다.
        // (예전에는 하나만 받아도 꺼져서, 남은 보상이 있는데도 알림이 사라졌다)
        PhoneManager.Instance?.UpdateAppAlarmState(
            AppKey.Quest,
            HasOtherClaimableReward() ? AlarmState.NonMandatory : AlarmState.None);

        SoundManager.Instance?.PlayEffect("QuestSuccess");

        RequestManager.Instance?.AddCompleteRequestCount();

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
