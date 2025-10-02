using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdResistenceUpgrade : Upgrade
{
    public override string Name => "추위 저항 확률 증가";
    public override string Explanation => "추위 형질을 회복하고, 추가 저항력(최대 15%)을 5% 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_16");
    public override int MaxAmount => -1;
    public override int UnlockStage => ColdWave.UnlockStage;
    public override int UpgradeId => 11;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.ColdResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
