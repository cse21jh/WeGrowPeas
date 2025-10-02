using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindResistenceUpgrade : Upgrade
{
    public override string Name => "바람 저항 확률 증가";
    public override string Explanation => "바람 형질을 회복하고, 추가 저항력(최대 15%)을 5% 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_14");
    public override int MaxAmount => -1;
    public override int UnlockStage => WindWave.UnlockStage;
    public override int UpgradeId => 8;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.WindResistance, 0.05f, true);
        Debug.Log(Explanation);
    }
}
