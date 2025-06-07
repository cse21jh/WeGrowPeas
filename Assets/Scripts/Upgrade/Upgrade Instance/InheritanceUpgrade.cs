using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InheritanceUpgrade : Upgrade
{
    public override string Name => "우수 형질 확률 증가";
    public override string Explanation => "형질이 1인 개체 교배 시, 우수한 형질이 나올 확률이 10% 증가합니다.";
    public override int MaxAmount => 2;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalInheritance(10);
    }
}
