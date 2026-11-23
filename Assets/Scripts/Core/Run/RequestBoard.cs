using System;
using System.Collections.Generic;

/// <summary>의뢰가 세계에서 알아야 하는 것.</summary>
public interface IRequestWorld
{
    RunEvents Events { get; }
    /// <summary>지금 해금된 웨이브 중 하나를 뽑아 형질 번호로.</summary>
    int PickTraitFromWaves();
    /// <summary>밤의 자유시간(폰 시간)인가.</summary>
    bool IsFreeTime { get; }
}

/// <summary>한 판의 의뢰 게시판. 5일마다 새 의뢰를 뽑는다.</summary>
public sealed class RequestBoard
{
    private readonly IReadOnlyList<RequestScriptable> pool;
    private readonly int cycle;
    private readonly int requestNum;
    private readonly List<RequestInstance> active = new List<RequestInstance>();

    public RequestBoard(IReadOnlyList<RequestScriptable> pool, int cycle, int requestNum, IRequestWorld world)
    {
        this.pool = pool;
        this.cycle = cycle;
        this.requestNum = requestNum;
        World = world;
    }

    public IRequestWorld World { get; }
    public IReadOnlyList<RequestInstance> Active => active;
    public int CycleEndRound { get; private set; } = -1;
    /// <summary>이번 주기가 시작된 뒤 지난 날.</summary>
    public int DayPassed { get; set; }
    public int CompletedCount { get; set; }

    public event Action<RequestInstance> Changed;
    /// <summary>의뢰 하나를 막 달성했다(보상은 아직).</summary>
    public event Action<RequestInstance> Completed;

    public void NotifyCompleted(RequestInstance request) => Completed?.Invoke(request);

    private const int CycleStartOffset = 2; // 2, 7, 12, ...일차

    public bool IsCycleStart(int stage) => stage % cycle == CycleStartOffset;

    public bool StartCycle(int stage) => StartCycle(stage, unchecked(GameRun.Current.Shop.UniqueSeed + stage * 100));

    /// <param name="seed">판의 상점 시드와 일차로 정한다. 같은 판의 같은 날이면 같은 의뢰가 나온다.</param>
    /// <returns>뽑을 의뢰가 있었는가.</returns>
    public bool StartCycle(int stage, int seed)
    {
        CycleEndRound = stage + cycle - 1;
        DayPassed = 0;
        ClearActive();

        var valid = new List<RequestScriptable>();
        foreach (RequestScriptable data in pool)
            if (data != null && stage >= data.minStage) valid.Add(data);
        if (valid.Count == 0) return false;

        var rng = new Random(seed);
        var picked = new List<RequestScriptable>();
        for (int safety = 2000; picked.Count < requestNum && safety > 0; safety--)
        {
            RequestScriptable candidate = valid[rng.Next(valid.Count)];
            if (!picked.Exists(p => p.requestId == candidate.requestId)) picked.Add(candidate);
        }

        foreach (RequestScriptable data in picked) Add(data);
        return true;
    }

    private RequestInstance Add(RequestScriptable data)
    {
        RequestInstance request = Create(data);
        if (request == null) return null;

        request.Board = this;
        request.OnChanged += HandleChanged;
        request.Start();
        active.Add(request);
        return request;
    }

    private void HandleChanged(RequestInstance request) => Changed?.Invoke(request);

    public void ClearActive()
    {
        foreach (RequestInstance request in active)
        {
            request.OnChanged -= HandleChanged;
            request.Stop();
        }
        active.Clear();
    }

    private static RequestInstance Create(RequestScriptable data)
    {
        switch (data.requestId.Substring(0, 3))
        {
            case "000": return new MovePlantRequest(data);
            case "001": return new SaveBreedingCountRequest(data);
            case "002": return new FeedBugRequest(data);
            case "003": return new NoSellDayRequest(data);
            case "004": return new BuyItemInDayRequest(data);
            case "005": return new SellHighResistancePlantRequest(data);
            case "006": return new SellGrayGenePlantRequest(data);
            case "007": return new SellPlantFreeTimeRequest(data);
            case "008": return new SellLowResistancePlantRequest(data);

            case "990": return new LegacyKillBugRequest(data);
            case "991": return new LegacyPeaBreedingRequest(data);
            case "992": return new LegacyPeaSurviveRequest(data);
            case "993": return new LegacyNoSellPeaRequest(data);
            case "994": return new LegacyBuyMerchRequest(data);
            case "995": return new LegacySpendGoldRequest(data);
            case "996": return new LegacySellSpecificPeaRequest(data);

            default: return null;
        }
    }

    public void Save(RequestSave save)
    {
        save.cycleEndRound = CycleEndRound;
        save.dayPassed = DayPassed;
        save.completeRequestCount = CompletedCount;

        save.activeRequests.Clear();
        foreach (RequestInstance request in active) save.activeRequests.Add(request.ToSaveData());
    }

    public void Load(RequestSave save)
    {
        ClearActive();
        CycleEndRound = save.cycleEndRound;
        DayPassed = save.dayPassed;
        if (save.activeRequests == null || save.activeRequests.Count == 0) return;

        foreach (RequestInstanceSaveData data in save.activeRequests)
        {
            RequestScriptable found = null;
            foreach (RequestScriptable candidate in pool)
                if (candidate != null && candidate.requestId == data.requestId) { found = candidate; break; }
            if (found != null) Add(found)?.LoadFromSaveData(data);
        }
        CompletedCount = save.completeRequestCount;
    }
}
