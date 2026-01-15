using Palmmedia.ReportGenerator.Core.CodeAnalysis;
using UnityEngine;

public class KillBugRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public KillBugRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnBugKilled += HandleBugKilled;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnBugKilled -= HandleBugKilled;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleBugKilled()
    {
        if (IsCompleted) return;

        currentCount++;
        Debug.Log("현재 목표량 " + requiredCount + "까지 " + currentCount + "잡았습니다.");
        
        if (currentCount == requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 10,
            2 => 20,
            3 => 30,
            _ => 10,
        };
    }

    public override RequestInstanceSaveData ToSaveData()
    {
        return new RequestInstanceSaveData
        {
            requestId = Data.requestId,
            typeCode = Data.requestId.Substring(0, 3),
            progressCount = currentCount,
            isCompleted = IsCompleted,
            isRewardGranted = rewardGranted
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        IsCompleted = data.isCompleted;
        rewardGranted = data.isRewardGranted;

        RaiseChanged();
    }
}
