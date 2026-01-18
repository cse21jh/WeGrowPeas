using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class SellSpecificPeaRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int conditionType;
    private string[] traitName = { "자연사", "해충", "바람", "홍수", "폭우", "추위", "가뭄", "더위" };

    public SellSpecificPeaRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        SelectRandomTrait();

        GameEvents.OnPeaSold += HandleCheckType;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnPeaSold -= HandleCheckType;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    public override string GetTitleText()
    {
        return Data.requestTitle.Replace("{Trait}", traitName[conditionType]);
    }

    private void HandleCheckType(Plant p)
    {
        if (IsCompleted || IsFailed) return;

        //plant의 trait와 conditionType 비교
        List<GeneticTrait> traits = p.GetGeneticTrait();
        if (traits[conditionType].genetics < 2) return;

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
            1 => 6,
            2 => 9,
            3 => 12,
            _ => 100,
        };
    }

    private void SelectRandomTrait()
    {
        conditionType = Random.Range((int)TraitType.NaturalDeath, (int)(TraitType.Heat) + 1);
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
