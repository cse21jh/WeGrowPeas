using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainResistenceUpgrade : Upgrade
{
    public override string Name => "폭우 저항 확률 증가";
    public override string Explanation => "폭우에 저항할 확률이 3% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_11");
    public override int MaxAmount => 5;
    public override int UnlockStage => 25;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.HeavyRainResistance, 0.03f);
        Debug.Log(Explanation);
    }
}
