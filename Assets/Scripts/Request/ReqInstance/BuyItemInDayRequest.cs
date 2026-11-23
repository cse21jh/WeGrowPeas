using UnityEngine;

public class BuyItemInDayRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private int startDayPassed = -1;

    public BuyItemInDayRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        startDayPassed = Board.DayPassed;

        Events.ShopBought += HandleShopBought;
        RaiseChanged();
    }

    public override void Stop()
    {
        Events.ShopBought -= HandleShopBought;
    }

    // 하루가 지나면 처음부터 다시 센다
    public override int Remaining => IsCompleted ? 0 : Board.DayPassed != startDayPassed ? requiredCount : requiredCount - currentCount;

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleShopBought(ItemData item)
    {
        if (IsCompleted || IsFailed) return;

        if (Board.DayPassed != startDayPassed)
        {
            currentCount = 0;
            startDayPassed = Board.DayPassed;
        }

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

        startDayPassed = Board.DayPassed;

        RaiseChanged();
    }
}
