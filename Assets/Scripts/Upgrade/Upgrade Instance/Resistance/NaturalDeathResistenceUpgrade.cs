using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaturalDeathResistenceUpgrade : Upgrade
{
    public override string Name => "자연사 저항 확률 증가";
    public override string Explanation => "농장의 식물이 자연사에 저항할 확률이 5% 증가합니다 (최대 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_12");
    public override int MaxAmount => -1;
    public override int UnlockStage => 6;
    public override int UpgradeId => 7;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.NaturalDeath, 0.05f);
        Debug.Log(Explanation);
    }
}
