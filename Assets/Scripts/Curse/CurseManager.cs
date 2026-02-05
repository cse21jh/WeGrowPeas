using System;
using System.Collections.Generic;
using UnityEngine;

public class CurseManager : Singleton<CurseManager>
{
    [SerializeField] private List<CurseScriptable> temporalCursePool = new();

    [Header("Curse Related Objects")]
    [SerializeField] private GameObject fog;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void SelectCurse(int stage)
    {
        SelectTemporalCurse();
        if (stage % 5 == 0) SelectSeasonalCurse();
    }

    public void ApplyCurse()
    {
        ApplyTemporalCurse();
        ApplySeasonalCurse();
    }

    private void SelectTemporalCurse()
    {
        Debug.Log("단발형 저주 생성!");
    }

    private void ApplyTemporalCurse()
    {

    }

    private void SelectSeasonalCurse()
    {
        Debug.Log("지속형 저주 설정!");
    }

    private void ApplySeasonalCurse()
    {

    }

}
