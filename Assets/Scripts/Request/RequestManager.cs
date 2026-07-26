using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[System.Serializable]
public class RequestInstanceSaveData
{
    public string requestId;
    public string typeCode;
    public int progressCount; // 진행도 저장
    public int state;
    public List<string> extraStrings; //buymerch
    public int extraInt; //sellspecificpea & peasurvive
}

public class RequestManager : Singleton<RequestManager>
{
    public int cycle = 5;
    public int requestNum = 3;
    private int dayPassed = 0;
    public int DayPassed => dayPassed;

    [SerializeField] private TextMeshProUGUI appTitle;

    private bool newRequestArrived = false;

    [SerializeField] private List<RequestScriptable> requestPool = new();

    private readonly List<RequestInstance> activeReq = new();
    public IReadOnlyList<RequestInstance> ActiveReq => activeReq;

    private readonly HashSet<RequestInstance> rewardGranted = new();

    private int cycleEndRound = -1;
    public int CycleEndRound => cycleEndRound;

    public event Action OnBoardUpdated;
    public event Action OnProgressUpdated;

    private int completeRequestCount = 0;

    public int CompleteRequestCount => completeRequestCount;

    private void OnEnable()
    {
        GameEvents.OnDayPassedForRequest += HandleDayPassed;
    }

    private void OnDisable()
    {
        GameEvents.OnDayPassedForRequest -= HandleDayPassed;
        ClearActive();
    }

    private void Start()
    {

    }

    private void HandleDayPassed()
    {
        dayPassed++;
        UpdateRequestAppTitle();
    }

    public void StartNewCycle(int startRound)
    {
        cycleEndRound = startRound + cycle - 1;
        dayPassed = 0;

        ClearActive();
        GenerateRandomRequests();
        UpdateRequestAppTitle();

        OnBoardUpdated?.Invoke();
    }

    private void GenerateRandomRequests()
    {
        ClearRequestAlarm();

        // requestId 중복 방지 및 특정 퀘스트 필터링
        var valid = requestPool.Where(p => p != null && !(p.requestId.StartsWith("002") && GameManager.Instance.stage < 11)).ToList();

        if (valid.Count == 0) return;

        int seed = Environment.TickCount;
        if (GameManager.Instance != null && ShopManager.Instance != null)
        {
            seed = ShopManager.Instance.GetGameUniqueSeed() + GameManager.Instance.stage * 100;
        }
        var rng = new System.Random(seed);
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

        PushRequestGenerateAlarm();
    }

    private RequestInstance CreateInstanceById(RequestScriptable data)
    {
        string typeCode = data.requestId.Substring(0, 3);

        return typeCode switch
        {
            "000" => new MovePlantRequest(data),
            "001" => new SaveBreedingCountRequest(data),
            "002" => new FeedBugRequest(data),
            "003" => new NoSellDayRequest(data),
            "004" => new BuyItemInDayRequest(data),
            "005" => new SellHighResistancePlantRequest(data),
            "006" => new SellGrayGenePlantRequest(data),
            "007" => new SellPlantFreeTimeRequest(data),
            "008" => new SellLowResistancePlantRequest(data),

            "990" => new LegacyKillBugRequest(data),
            "991" => new LegacyPeaBreedingRequest(data),
            "992" => new LegacyPeaSurviveRequest(data),
            "993" => new LegacyNoSellPeaRequest(data),
            "994" => new LegacyBuyMerchRequest(data),
            "995" => new LegacySpendGoldRequest(data),
            "996" => new LegacySellSpecificPeaRequest(data),

            _ => null
        };
    }

    private void HandleRequestChanged(RequestInstance _)
    {
        OnProgressUpdated?.Invoke();
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
        var data = new List<RequestInstanceSaveData>();

        foreach (var req in activeReq)
        {
            if (req != null)
            {
                data.Add(req.ToSaveData());
            }
        }

        return data;
    }

    public void LoadRequestManager(SaveData saveData)
    {
        ClearActive();

        cycleEndRound = saveData.cycleEndRound;
        dayPassed = saveData.dayPassed;

        if (saveData.activeRequests == null || saveData.activeRequests.Count == 0) return;

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

        completeRequestCount = saveData.completeRequestCount;

        OnBoardUpdated?.Invoke();
    }

    private RequestScriptable FindScriptableById(string id)
    {
        return requestPool.FirstOrDefault(p => p != null && p.requestId == id);
    }

    private void ClearRequestAlarm()
    {
        PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.None);
    }

    private void PushRequestGenerateAlarm()
    {
        PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = "새 퀘스트 도착!",
                        message = "퀘스트 앱에서 확인해 주세요.",
                        duration = 5f
                    }
                );

        if (PhoneManager.Instance.CurrentApp == AppKey.Quest)
        {
            newRequestArrived = false;
        }
        else
        {
            newRequestArrived = true;
            PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.NonMandatory);
        }
    }

    public void OnCheckedNewRequest()
    {
        if (newRequestArrived)
        {
            newRequestArrived = false;
            PhoneManager.Instance.UpdateAppAlarmState(AppKey.Quest, AlarmState.None);
        }
    }

    public void UpdateRequestAppTitle()
    {
        if (cycleEndRound < 0) return;

        appTitle.text = "퀘스트 - " + (cycle - dayPassed) + "일 남음";
    }

    public void AddCompleteRequestCount()
    {
        completeRequestCount++;
    }
}
