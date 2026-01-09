using UnityEngine;

public class KillBugRequest : RequestInstance
{
    private int requiredCount;
    private int currentCount;

    public KillBugRequest(RequestScriptable data) : base(data)
    {
        requiredCount = SetDifficulty(data.requestId);
    }

    public override void Start()
    {
        base.Start();
        currentCount = 0;

        GameEvents.OnBugKilled += HandleBugKilled;
        RaiseChanged();
    }

    public override void Stop()
    {
        GameEvents.OnBugKilled -= HandleBugKilled;
    }

    private void HandleBugKilled()
    {
        if (IsCompleted) return;

        currentCount++;
        Debug.Log("현재 목표량 " + requiredCount + "까지 " + currentCount + "잡았습니다.");
        
        if (currentCount == requiredCount) CompleteOnce();
        else RaiseChanged();
    }

    private int SetDifficulty(string requestId)
    {
        char c = requestId[3];

        int difficulty = c - '0';

        return difficulty switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            _ => 10,
        };
    }
}
