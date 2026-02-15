using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CurseManager : Singleton<CurseManager>
{
    [SerializeField] private List<CurseScriptable> temporalCursePool = new();

    [Header("Curse Related Objects")]
    [SerializeField] private GameObject fog;

    private float tempCursePercentage = 0.2f; //test 시 1로 설정

    public CurseInstance currentTempCurse = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void OnEnable()
    {
        GameEvents.OnDayPassedForRequest += RemoveCurse;
    }

    private void OnDisable()
    {
        GameEvents.OnDayPassedForRequest -= RemoveCurse;
    }

    public void SelectCurse(int stage)
    {
        SelectTemporalCurse();
        //if (stage % 5 == 0) SelectSeasonalCurse();
    }

    public void ApplyCurse()
    {
        ApplyTemporalCurse();
        ApplySeasonalCurse();
    }

    private void SelectTemporalCurse()
    {
        Debug.Log("CurseManager입니다.");
        float rd = UnityEngine.Random.value;
        if (rd > tempCursePercentage)
        {
            Debug.Log(rd);
            return;
        }

        Debug.Log("단발형 저주 생성!");

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
        Debug.Log("지속형 저주 설정!");
    }

    private void ApplySeasonalCurse()
    {

    }

    private void RemoveCurse()
    {
        if (currentTempCurse == null) return;

        Debug.Log("저주 해제!");
        currentTempCurse.Deactivate();
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

            _ => null
        };
    }

    public string SaveCurseManager()
    {
        if(currentTempCurse != null)
            return currentTempCurse.Data.curseId;

        return null;
    }

    public void LoadCurseManager(SaveData saveData)
    {
        var scriptable = FindScriptableById(saveData.tempCurseId);

        if(scriptable != null) currentTempCurse = CreateInstanceById(scriptable);
        else currentTempCurse = null;
    }

    private CurseScriptable FindScriptableById(string id)
    {
        return temporalCursePool.FirstOrDefault(p => p != null && p.curseId == id);
    }
}
