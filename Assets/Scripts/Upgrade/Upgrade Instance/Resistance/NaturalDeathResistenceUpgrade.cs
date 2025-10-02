using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaturalDeathResistenceUpgrade : Upgrade
{
    public override string Name => "자연사 저항 확률 증가";
    public override string Explanation => "자연사 형질을 회복하고, 추가 저항력(최대 15%)을 5% 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_12");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 7;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.NaturalDeath, 0.05f);
        Debug.Log(Explanation);
    }
}
