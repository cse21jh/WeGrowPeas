using UnityEngine;

public class MovePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public MovePlantRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnPlantMoved += HandlePlantMoved;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPlantMoved -= HandlePlantMoved;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandlePlantMoved()
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
            1 => 10,
            2 => 15,
            3 => 20,
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
