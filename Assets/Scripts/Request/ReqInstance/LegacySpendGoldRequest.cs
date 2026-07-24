using UnityEngine;

public class LegacySpendGoldRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public LegacySpendGoldRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnShopBought += HandleGoldSpent;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnShopBought -= HandleGoldSpent;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleGoldSpent(ItemData item)
    {
        if (IsCompleted || IsFailed) return;

        currentCount += item.Price;

        if (currentCount >= requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 4000,
            2 => 7000,
            3 => 10000,
            _ => 100,
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
