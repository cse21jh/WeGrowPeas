using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainResistenceUpgrade : Upgrade
{
    public override string Name => "폭우 저항 확률 증가";
    public override string Explanation => "폭우 형질을 회복하고, 추가 저항력(최대 15%)을 5% 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_17");
    public override int MaxAmount => -1;
    public override int UnlockStage => HeavyRainWave.UnlockStage;
    public override int UpgradeId => 12;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(TraitType.HeavyRain, 0.05f, true);
        Debug.Log(Explanation);
    }
}
