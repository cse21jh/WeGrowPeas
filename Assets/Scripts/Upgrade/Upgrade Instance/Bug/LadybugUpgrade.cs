using UnityEngine;

public class LadybugUpgrade : Upgrade
{
    public override string Name => "익충 등장 확률 증가";
    public override string Explanation => "익충 등장 확률이 10% 증가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 4;
    public override int UnlockStage => 11;
    public override int UpgradeId => 20;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddLadybugSpawnProbability(0.1f);
    }
}
