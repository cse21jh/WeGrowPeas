using UnityEngine;

public class BugSpeedUpgrade : Upgrade
{
    public override string Name => "벌레 속도 감소";
    public override string Explanation => "벌레의 속도가 10% 감소합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 3;
    public override int UnlockStage => 10;
    public override int UpgradeId => 19;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddBugSpeedDcreasement(0.1f);
    }
}
