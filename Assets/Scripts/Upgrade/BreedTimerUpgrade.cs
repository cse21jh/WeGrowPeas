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
        Debug.Log("10초 더하기!"); // 실제로 gameManager.Instance 식으로 불러올 수 있도록 싱글톤 제작 필요
    }
}
