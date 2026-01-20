using UnityEngine;

public class PeaSurviveRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int date;

    public PeaSurviveRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        date = 5;

        GameEvents.OnPeaDied += HandlePeaDied;
        GameEvents.OnDayPassedForRequest += HandleDayPassed;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPeaDied -= HandlePeaDied;
        GameEvents.OnDayPassedForRequest += HandleDayPassed;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandlePeaDied()
    {
        if (IsCompleted || IsFailed) return;

        currentCount++;

        if (currentCount == requiredCount) MarkFailed();
        else RaiseChanged();
    }

    private void HandleDayPassed()
    {
        date--;

        if (date == 0 && !IsFailed) CompleteOnce();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 30,
            2 => 25,
            3 => 20,
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
            extraInt = date,
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;
        date = data.extraInt;

        RaiseChanged();
    }
}
