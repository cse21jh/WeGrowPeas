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

        GameEvents.OnPeaSold += HandlePeaSold;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPeaSold -= HandlePeaSold;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandlePeaSold(Plant p)
    {
        if (IsCompleted || IsFailed) return;

        bool hasLowRes = false;
        foreach (var t in p.GetGeneticTrait())
        {
            if (p.GetResistanceValue((int)t.traitType) <= targetResistance)
            {
                hasLowRes = true;
                break;
            }
        }

        if (hasLowRes)
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
