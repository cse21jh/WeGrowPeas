using UnityEngine;

public class LegacyNoSellPeaRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private bool isPeaSoldToday;

    public LegacyNoSellPeaRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        isPeaSoldToday = false;

        GameEvents.OnDayPassedForRequest += HandleDayPassed;
        GameEvents.OnPeaSold += HandlePeaSold;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnDayPassedForRequest -= HandleDayPassed;
        GameEvents.OnPeaSold -= HandlePeaSold;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleDayPassed()
    {
        if (State != RequestState.InProgress) return;

        if (isPeaSoldToday)
        {
            isPeaSoldToday = false;
            return;
        }
        else currentCount++;

        if (currentCount == requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private void HandlePeaSold(Plant p)
    {
        if (IsCompleted || IsFailed) return;

        currentCount = 0;
        isPeaSoldToday = true;

        RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 2,
            2 => 3,
            3 => 4,
            _ => 100,
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
        isPeaSoldToday = false;

        RaiseChanged();
    }
}
