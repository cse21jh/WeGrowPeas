using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaxBreedCountUpgrade : Upgrade
{
    public override string Name => "교배 가능 횟수 증가";
    public override string Explanation => "교배 가능 횟수가 1회 증가합니다";
    public override int MaxAmount => 6;
    public override int UnlockStage => 10;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddMaxBreedCount(1);
        Debug.Log(Explanation);
    }
}

