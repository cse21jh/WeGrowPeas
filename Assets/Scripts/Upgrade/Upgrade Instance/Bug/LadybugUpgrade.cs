using UnityEngine;

public class LadybugUpgrade : Upgrade
{
    public override string Name => "무당벌레 등장 확률 증가";
    public override string Explanation => "해충을 잡아먹는 무당벌레가 등장할 확률이 5% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_27");
    public override int MaxAmount => 4;
    public override int UnlockStage => 11;
    public override int UpgradeId => 20;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddLadybugSpawnProbability(0.05f);
    }
}
