using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestResistenceUpgrade : Upgrade
{
    public override string Name => "해충 저항 확률 증가";
    public override string Explanation => "해충에 저항할 확률이 3% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 5;
    public override int UnlockStage => 999999;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.PestResistance, 0.03f);
        Debug.Log(Explanation);
    }
}
