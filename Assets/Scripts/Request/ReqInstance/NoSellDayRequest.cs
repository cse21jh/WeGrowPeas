using UnityEngine;

public class NoSellDayRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private bool isPeaSoldToday;

    public NoSellDayRequest(RequestScriptable data) : base(data)
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
        if (IsCompleted || IsFailed) return;

        if (!isPeaSoldToday)
        {
            currentCount++;
            if (currentCount >= requiredCount) CompleteOnce();
            else RaiseChanged();
        }

        isPeaSoldToday = false;
    }

    private void HandlePeaSold(Plant p)
    {
        if (IsCompleted || IsFailed) return;
        isPeaSoldToday = true;
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 1,
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
            extraInt = isPeaSoldToday ? 1 : 0
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;
        isPeaSoldToday = data.extraInt == 1;

        RaiseChanged();
    }
}
