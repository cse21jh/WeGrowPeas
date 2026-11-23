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

    private void HandlePeaSold(PlantState p)
    {
        if (IsCompleted || IsFailed) return;

        List<GeneticTrait> traits = p.Traits;
        foreach (var g in traits)
        {
            if (conditionType == (int)g.traitType)
            {
                if (g.genetics == 0) // 회색 유전자 2개 (우성 0개)
                {
                    currentCount++;
                    if (currentCount >= requiredCount) CompleteOnce();
                    else RaiseChanged();
                }
                return;
            }
        }
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
