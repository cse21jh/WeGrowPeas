using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreedTimerUpgrade : Upgrade
{
    public override string Name => "교배 시간 증가";
    public override string Explanation => "교배 가능 시간이 10초 증가합니다";
    public override int MaxAmount => 2;
    public override void OnSelectAction() 
    {
        GameManager.Instance.grid.AddBreedTimer(10);
        Debug.Log(GameManager.Instance.grid.breedTimer);
    }
}
