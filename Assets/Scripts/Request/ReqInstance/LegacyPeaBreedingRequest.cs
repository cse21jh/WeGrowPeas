using UnityEngine;

public class LegacyPeaBreedingRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public LegacyPeaBreedingRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnPeaBreeded += HandlePeaBreeded;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPeaBreeded -= HandlePeaBreeded;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandlePeaBreeded()
    {
        if (IsCompleted) return;

        currentCount++;

        if (currentCount == requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 15,
            2 => 20,
            3 => 30,
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

        RaiseChanged();
    }
}
