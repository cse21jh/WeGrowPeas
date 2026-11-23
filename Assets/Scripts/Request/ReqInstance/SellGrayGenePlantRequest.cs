using System.Collections.Generic;
using UnityEngine;

public class SellGrayGenePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int conditionType;
    private string WaveLabel => WavePalette.GetName((TraitType)conditionType);

    public SellGrayGenePlantRequest(RequestScriptable data) : base(data)
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

    // 회색 유전자 2개 (우성 0개)
    public override bool AcceptsSale(PlantState plant)
    {
        foreach (var g in plant.Traits)
            if (conditionType == (int)g.traitType) return g.genetics == 0;
        return false;
    }

    private void HandlePeaSold(PlantState p)
    {
        if (IsCompleted || IsFailed) return;

        if (!AcceptsSale(p)) return;

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
