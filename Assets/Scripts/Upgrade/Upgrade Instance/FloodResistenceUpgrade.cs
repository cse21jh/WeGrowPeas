using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodResistenceUpgrade : Upgrade
{
    public override string Name => "홍수 저항 확률 증가";
    public override string Explanation => "홍수에 저항할 확률이 5% 증가합니다";
    public override int MaxAmount => 1;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.FloodResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
