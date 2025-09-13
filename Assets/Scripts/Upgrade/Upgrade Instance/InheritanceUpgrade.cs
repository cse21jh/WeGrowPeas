using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InheritanceUpgrade : Upgrade
{
    public override string Name => "우수 형질 확률 증가";
    public override string Explanation => "우수한 형질이 나올 확률이 10% 증가합니다.";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_15");
    public override int MaxAmount => 2;
    public override int UnlockStage => 11;
    public override int UpgradeId => 16;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalInheritance(10);
    }
}
