using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSkipUpgrade : Upgrade
{
    public override string Name => "웨이브 스킵 횟수 증가";
    public override string Explanation => "웨이브 스킵 가능 횟수가 1회 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_23");
    public override int MaxAmount => -1;
    public override int UnlockStage => 10;
    public override int UpgradeId => 18;
    public override void OnSelectAction()
    {
        GameManager.Instance.enemyController.AddWaveSkipCount(1);
        Debug.Log(Explanation);
    }
}