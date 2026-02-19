using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CurseManager : Singleton<CurseManager>
{
    [SerializeField] private List<CurseScriptable> temporalCursePool = new();
    [SerializeField] private List<CurseScriptable> seasonalCursePool = new();

    [Header("Curse Related Objects")]
    [SerializeField] private GameObject fog;

    private float tempCursePercentage = 0.2f; //test 시 1로 설정
    private int seasonInterval = 5; //지속형 저주 설정 단위
    private int remainingCurseDay = 0; // 지속형 저주 남은 기간
    public int RemainingCurseDay => remainingCurseDay;

    public CurseInstance currentTempCurse = null;
    public CurseInstance currentSeasonCurse = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void OnEnable()
    {
        GameEvents.OnDayPassedForRequest += RemoveCurse;
        GameEvents.OnDayPassedForRequest += UpdateSeasonalCurse;
    }

    private void OnDisable()
    {
        GameEvents.OnDayPassedForRequest -= RemoveCurse;
        GameEvents.OnDayPassedForRequest -= UpdateSeasonalCurse;
    }

    public void SelectCurse(int stage)
    {
        SelectTemporalCurse();
        if (stage % seasonInterval == 0) SelectSeasonalCurse();
    }

    public void ApplyCurse()
    {
        ApplyTemporalCurse();
        ApplySeasonalCurse();
    }

    private void SelectTemporalCurse()
    {
        //Debug.Log("CurseManager입니다.");
        float rd = UnityEngine.Random.value;
        if (rd > tempCursePercentage)
        {
            Debug.Log(rd);
            return;
        }

        //Debug.Log("단발형 저주 생성!");

        var data = temporalCursePool[UnityEngine.Random.Range(0, temporalCursePool.Count)];
        currentTempCurse = CreateInstanceById(data);
    }

    private void ApplyTemporalCurse()
    {
        if(currentTempCurse == null) return;

        currentTempCurse.Activate();
    }

    private void SelectSeasonalCurse()
    {
        var data = seasonalCursePool[UnityEngine.Random.Range(0, seasonalCursePool.Count)];
        currentSeasonCurse = CreateInstanceById(data);

        Debug.Log("지속형 저주 설정!");
        //다음 지속형 저주에 대한 경고 UI 표시
    }

    private void ApplySeasonalCurse()
    {
        if(currentSeasonCurse == null) return;

        currentSeasonCurse.Activate();
    }

    private void RemoveCurse()
    {
        if (currentTempCurse == null) return;

        Debug.Log("저주 해제!");
        currentTempCurse.Deactivate();
    }

    private void UpdateSeasonalCurse()
    {
        remainingCurseDay--;

        if(remainingCurseDay == 0)
        {
            currentSeasonCurse.Deactivate();
            currentSeasonCurse = null;
            return;
        }
    }

    private CurseInstance CreateInstanceById(CurseScriptable data)
    {
        string typeCode = data.curseId;

        return typeCode switch
        {
            "101" => new ReverseCurse(data),
            "102" => new FogCurse(data),
            "103" => new ThiefCurse(data),
            "104" => new WaveBlindCurse(data),
            "105" => new MushroomCurse(data),
            "106" => new BreedMadnessCurse(data),
            "107" => new RearrangeCurse(data),
            "108" => new DoubleWaveCurse(data),
            "109" => new EMPCurse(data),

            //"201" => new BugFestivalCurse(data),
            //"202" => new MutationCurse(data),
            //"203" => new RadiationCurse(data),
            //"204" => new PollenLostCurse(data),
            //"205" => new ShopMonopolyCurse(data),
            //"206" => new InsomniaCurse(data),
            //"207" => new SeedlessCurse(data),
            //"208" => new HeavyFireCurse(data),

            _ => null
        };
    }

    public string[] SaveCurseManager()
    {
        string[] cId = new string[2];

        cId[0] = currentTempCurse != null ? currentTempCurse.Data.curseId : null;
        cId[1] = currentSeasonCurse != null ? currentSeasonCurse.Data.curseId : null;

        return cId;
    }

    public void LoadCurseManager(SaveData saveData)
    {
        var scriptable = FindScriptableById(saveData.curseId[0]); //temp

        if(scriptable != null) currentTempCurse = CreateInstanceById(scriptable);
        else currentTempCurse = null;

        scriptable = FindScriptableById(saveData.curseId[1]); //season

        if (scriptable != null) currentSeasonCurse = CreateInstanceById(scriptable);
        else currentSeasonCurse = null;

        remainingCurseDay = saveData.remainSeasonCurseDay;
    }

    private CurseScriptable FindScriptableById(string id)
    {
        return temporalCursePool.FirstOrDefault(p => p != null && p.curseId == id);
    }
}
