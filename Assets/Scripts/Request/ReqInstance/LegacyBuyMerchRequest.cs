using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class LegacyBuyMerchRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;
    private HashSet<string> boughtItems = new HashSet<string>();

    public LegacyBuyMerchRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;
        boughtItems.Clear();

        GameEvents.OnShopBought += HandleMerchBought;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnShopBought -= HandleMerchBought;
    }

    public override string GetProgressText()
    {
        return currentCount + "/" + requiredCount;
    }

    private void HandleMerchBought(ItemData item)
    {
        if (IsCompleted || IsFailed) return;

        bool isNewItem = boughtItems.Add(item.name);

        if (!isNewItem) return;

        currentCount = boughtItems.Count;

        if (currentCount == requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            1 => 5,
            2 => 8,
            3 => 11,
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
            extraStrings = boughtItems.ToList(),
        };
    }

    public override void LoadFromSaveData(RequestInstanceSaveData data)
    {
        currentCount = data.progressCount;
        State = (RequestState)data.state;

        boughtItems.Clear();
        if(data.extraStrings != null)
        {
            foreach (var names in data.extraStrings) boughtItems.Add(names);
        }

        RaiseChanged();
    }
}
