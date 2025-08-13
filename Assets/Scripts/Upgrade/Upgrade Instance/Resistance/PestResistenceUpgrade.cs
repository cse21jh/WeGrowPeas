using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestResistenceUpgrade : Upgrade
{
    public override string Name => "해충 저항 확률 증가";
    public override string Explanation => "농장의 식물이 해충에 저항할 확률이 5% 증가합니다 (최대 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 5;
    public override int UnlockStage => 999999;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.PestResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
