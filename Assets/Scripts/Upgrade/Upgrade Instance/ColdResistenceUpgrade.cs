using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdResistenceUpgrade : Upgrade
{
    public override string Name => "추위 저항 확률 증가";
    public override string Explanation => "추위에 저항할 확률이 3% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_10");
    public override int MaxAmount => 5;
    public override int UnlockStage => 20;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.ColdResistance, 0.03f);
        Debug.Log(Explanation);
    }
}
