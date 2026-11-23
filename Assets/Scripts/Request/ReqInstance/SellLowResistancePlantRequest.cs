using UnityEngine;

public class SellLowResistancePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private float targetResistance;

    public SellLowResistancePlantRequest(RequestScriptable data) : base(data)
    {
        targetResistance = SetDifficulty(data.requestId);
        requiredCount = 1;
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        Events.PlantSold += HandlePeaSold;
        RaiseChanged();
    }

    public override void Stop()
    {
        Events.PlantSold -= HandlePeaSold;
    }

    public override int Remaining => IsCompleted ? 0 : requiredCount - currentCount;

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    public override bool AcceptsSale(PlantState plant)
    {
        foreach (var t in plant.Traits)
            if (plant.GetResistanceValue((int)t.traitType) <= targetResistance) return true;
        return false;
    }

    private void HandlePeaSold(PlantState p)
    {
        if (IsCompleted || IsFailed) return;

        if (AcceptsSale(p))
        {
            currentCount++;
            if (currentCount >= requiredCount) CompleteOnce();
            else RaiseChanged();
        }
    }

    private float SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 0.4f,
            2 => 0.3f,
            3 => 0.2f,
            _ => 0.4f,
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
