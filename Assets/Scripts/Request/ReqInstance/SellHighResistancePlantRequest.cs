using UnityEngine;

public class SellHighResistancePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int conditionType;
    private string WaveLabel => WavePalette.GetName((TraitType)conditionType);

    public SellHighResistancePlantRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        SelectRandomTrait();

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

    public override string GetTitleText()
    {
        return Data.GetTitle(("wave", WaveLabel));
    }

    public override string GetDescriptionText()
    {
        return Data.GetDescription(("wave", WaveLabel));
    }

    private void SelectRandomTrait()
    {
        conditionType = Board.World.PickTraitFromWaves();
    }

    public override bool AcceptsSale(PlantState plant) => plant.GetResistanceValue(conditionType) >= 0.6f;

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

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 3,
            2 => 5,
            3 => 7,
            _ => 3,
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
            extraInt = conditionType,
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;
        conditionType = data.extraInt;

        RaiseChanged();
    }
}
