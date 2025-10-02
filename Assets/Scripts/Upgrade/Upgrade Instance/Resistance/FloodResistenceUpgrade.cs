using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodResistenceUpgrade : Upgrade
{
    public override string Name => "홍수 저항 확률 증가";
    public override string Explanation => "홍수 형질을 회복하고, 추가 저항력(최대 15%)을 5% 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_15");
    public override int MaxAmount => -1;
    public override int UnlockStage => FloodWave.UnlockStage;
    public override int UpgradeId => 9;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.FloodResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
