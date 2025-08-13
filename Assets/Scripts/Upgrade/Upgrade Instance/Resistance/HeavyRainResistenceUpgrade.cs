using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainResistenceUpgrade : Upgrade
{
    public override string Name => "폭우 저항 확률 증가";
    public override string Explanation => "농장의 식물이 폭우에 저항할 확률이 5% 증가합니다 (최대 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_11");
    public override int MaxAmount => -1;
    public override int UnlockStage => 25;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.HeavyRainResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
