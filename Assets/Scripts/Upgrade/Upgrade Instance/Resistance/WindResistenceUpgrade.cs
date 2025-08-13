using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindResistenceUpgrade : Upgrade
{
    public override string Name => "바람 저항 확률 증가";
    public override string Explanation => "농장의 식물이 바람에 저항할 확률이 5% 증가합니다 (최대 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_7");
    public override int MaxAmount => 5;
    public override int UnlockStage => 5;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.WindResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
