using UnityEngine;

public class SellHighResistancePlantRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int conditionType;
    private string[] waveName = { "자연사가", "해충이", "바람이", "홍수가", "폭우가", "가뭄이", "추위가", "더위가" };

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
        string rawWaveName = waveName[conditionType];
        string baseWaveName = rawWaveName.Substring(0, rawWaveName.Length - 1); // "폭우가" -> "폭우"
        return Data.requestTitle.Replace("(웨이브 중 하나)", baseWaveName);
    }

    public override string GetDescriptionText()
    {
        return Data.requestDescription.Replace("{Wave}", waveName[conditionType]);
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
