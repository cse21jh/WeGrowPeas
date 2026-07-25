using System.Collections.Generic;
using UnityEngine;

public class SellGrayGenePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int conditionType;
    private string[] traitName = { "자연사", "해충", "바람", "홍수", "폭우", "가뭄", "추위", "더위" };

    public SellGrayGenePlantRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        SelectRandomTrait();

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

    public override string GetTitleText()
    {
        return Data.requestTitle.Replace("(특정 형질)", traitName[conditionType]);
    }

    public override string GetDescriptionText()
    {
        return Data.requestDescription.Replace("{Trait}", traitName[conditionType]);
    }

    private void SelectRandomTrait()
    {
        if (GameManager.Instance != null && GameManager.Instance.enemyController != null)
            conditionType = GameManager.Instance.enemyController.PickTraitFromUnlockWave();
    }

    private void HandlePeaSold(Plant p)
    {
        if (IsCompleted || IsFailed) return;

        List<GeneticTrait> traits = p.GetGeneticTrait();
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
