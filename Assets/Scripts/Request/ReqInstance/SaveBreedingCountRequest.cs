using UnityEngine;

public class SaveBreedingCountRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public SaveBreedingCountRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnDayEndedWithRemainingBreeds += HandleDayEnded;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnDayEndedWithRemainingBreeds -= HandleDayEnded;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleDayEnded(int remainingBreeds)
    {
        if (IsCompleted || IsFailed) return;

        currentCount += remainingBreeds;
        
        if (currentCount >= requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 4,
            2 => 6,
            3 => 8,
            _ => 4,
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
