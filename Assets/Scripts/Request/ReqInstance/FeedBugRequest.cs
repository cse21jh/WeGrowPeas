using UnityEngine;

public class FeedBugRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public FeedBugRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnPeaDiedByBug += HandlePeaDiedByBug;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPeaDiedByBug -= HandlePeaDiedByBug;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandlePeaDiedByBug()
    {
        if (IsCompleted || IsFailed) return;

        currentCount++;
        
        if (currentCount >= requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 5,
            2 => 8,
            3 => 11,
            _ => 5,
        };
    }

    public override RequestInstanceSaveData ToSaveData()
    {
        return new RequestInstanceSaveData
        {
            requestId = Data.requestId,
            typeCode = Data.requestId.Substring(0, 3),
            progressCount = currentCount,
            state = (int)State,
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;

        RaiseChanged();
    }
}
