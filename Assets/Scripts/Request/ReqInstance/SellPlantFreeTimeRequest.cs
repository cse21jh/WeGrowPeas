using UnityEngine;

public class SellPlantFreeTimeRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public SellPlantFreeTimeRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

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

    private void HandlePeaSold(PlantState p)
    {
        if (IsCompleted || IsFailed) return;

        // 자유시간인지 체크 (핸드폰 페이즈)
        if (Board.World.IsFreeTime)
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
            1 => 6,
            2 => 9,
            3 => 12,
            _ => 6,
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
