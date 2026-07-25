using UnityEngine;

public class BuyItemInDayRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public BuyItemInDayRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnShopBought += HandleShopBought;
        GameEvents.OnDayPassedForRequest += HandleDayPassed;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnShopBought -= HandleShopBought;
        GameEvents.OnDayPassedForRequest -= HandleDayPassed;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleShopBought(ItemData item)
    {
        if (IsCompleted || IsFailed) return;

        currentCount++;
        
        if (currentCount >= requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private void HandleDayPassed()
    {
        if (IsCompleted || IsFailed) return;
        currentCount = 0;
        RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];
        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
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
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;

        RaiseChanged();
    }
}
