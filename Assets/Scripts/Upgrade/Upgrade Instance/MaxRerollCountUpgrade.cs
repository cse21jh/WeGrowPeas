using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaxRerollCountUpgrade : Upgrade
{
    public override string Name => "업그레이드 리롤 가능 횟수 증가";
    public override string Explanation => "업그레이드 리롤 가능 횟수가 1회 증가합니다";
    public override int MaxAmount => 2;
    public override void OnSelectAction()
    {
        GameManager.Instance.upgradeManager.AddMaxRerollCount(1);
        Debug.Log(Explanation);
    }
}

