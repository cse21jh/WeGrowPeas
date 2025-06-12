using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindResistenceUpgrade : Upgrade
{
    public override string Name => "바람 저항 확률 증가";
    public override string Explanation => "바람에 저항할 확률이 3% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_7");
    public override int MaxAmount => 5;
    public override int UnlockStage => 5;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.WindResistance, 0.03f);
        Debug.Log(Explanation);
    }
}
