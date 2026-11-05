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
        return Data.GetTitle(("wave", WaveLabel));
    }

    public override string GetDescriptionText()
    {
        return Data.GetDescription(("wave", WaveLabel));
    }

    private void SelectRandomTrait()
    {
        if (GameManager.Instance != null && GameManager.Instance.enemyController != null)
            conditionType = GameManager.Instance.enemyController.PickTraitFromUnlockWave();
    }

    private void HandlePeaSold(Plant p)
    {
        if (IsCompleted || IsFailed) return;

        if (p.GetResistanceValue(conditionType) >= 0.6f)
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
