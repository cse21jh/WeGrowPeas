using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RequestInstanceSaveData
{
    public string requestId;
    public string typeCode;
    public int progressCount; // 진행도 관련
    public bool isCompleted;
    public bool isRewardGranted;
}

public class RequestManager : Singleton<RequestManager>
{
    public int cycle = 5;
    public int requestNum = 3;

    [SerializeField] private List<RequestScriptable> requestPool = new();

    private readonly List<RequestInstance> activeReq = new();
    public IReadOnlyList<RequestInstance> ActiveReq => activeReq;

    private readonly HashSet<RequestInstance> rewardGranted = new();

    private int cycleEndRound = -1;
    public int CycleEndRound => cycleEndRound;

    public event Action OnBoardUpdated;

    private void OnEnable()
    {
        //GameEvents.OnRoundStarted += HandleRoundStarted;
    }

    private void OnDisable()
    {
        //GameEvents.OnRoundStarted -= HandleRoundStarted;
        //ClearActive();
    }

    private void Start()
    {
        
    }

    private void HandleRoundStarted(int round)
    {
        if (round > cycleEndRound)
        {
            StartNewCycle(round);
            return;
        }

        GrantRewardsForCompleted();
    }

    public void StartNewCycle(int startRound)
    {
        cycleEndRound = startRound + cycle - 1;

        ClearActive();
        GenerateRandomRequests();

        OnBoardUpdated?.Invoke();
    }

    private void GenerateRandomRequests()
    {
        var valid = requestPool.Where(p => p != null).ToList();

        if (valid.Count == 0) return;

        // requestId 중복 방지
        var rng = new System.Random();
        var picked = new List<RequestScriptable>();
        int safety = 2000;

        while (picked.Count < requestNum && safety-- > 0)
        {
            var cand = valid[rng.Next(valid.Count)];

            if (picked.Any(x => x.requestId == cand.requestId)) continue;
            picked.Add(cand);
        }

        foreach (var data in picked)
        {
            var req = CreateInstanceById(data);
            if (req == null) continue;

            req.OnChanged += HandleRequestChanged;
            req.Start();
            activeReq.Add(req);
        }
    }

    private RequestInstance CreateInstanceById(RequestScriptable data)
    {
        string typeCode = data.requestId.Substring(0, 3);

        return typeCode switch
        {
            "000" => new KillBugRequest(data),

            _ => null
        };
    }

    private void HandleRequestChanged(RequestInstance _)
    {
        OnBoardUpdated?.Invoke();
    }

    private void GrantRewardsForCompleted()
    {
        /*foreach (var req in activeReq)
        {
            if (!req.IsCompleted) continue;
            if (rewardGranted.Contains(req)) continue;

            req.GrantRewardOnce();
            rewardGranted.Add(req);
        }*/
    }

    private void ClearActive()
    {
        foreach (var req in activeReq)
        {
            req.OnChanged -= HandleRequestChanged;
            req.Stop();
        }

        activeReq.Clear();
        rewardGranted.Clear();
    }

    public List<RequestInstanceSaveData> getSaveData()
    {
        var data = new List<RequestInstanceSaveData> { new RequestInstanceSaveData() };

        if (activeReq.Count == 0) return data;

        foreach (var req in activeReq) data.Add(req.ToSaveData());

        return data;
    }

    public void LoadRequestManager(SaveData saveData)
    {
        ClearActive();

        cycleEndRound = saveData.cycleEndRound;

        if (saveData.activeRequests.Count == 0) return;

        foreach (var reqSave in saveData.activeRequests)
        {
            var scriptable = FindScriptableById(reqSave.requestId);
            if (scriptable == null) continue;

            var instance = CreateInstanceById(scriptable);
            if (instance == null) continue;

            instance.OnChanged += HandleRequestChanged;
            instance.Start();
            instance.LoadFromSaveData(reqSave);

            activeReq.Add(instance);
        }

        OnBoardUpdated?.Invoke();
    }

    private RequestScriptable FindScriptableById(string id)
    {
        return requestPool.FirstOrDefault(p => p != null && p.requestId == id);
    }
}
