using UnityEngine;

public class BugGoldUpgrade : Upgrade
{
    public override string Name => "¹ú·¹°¡ ÁÖ´Â µ· Áõ°¡";
    public override string Explanation => "¹ú·¹ÀÇ ÁÖ´Â µ·ÀÌ 10 Áõ°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 5;
    public override int UnlockStage => 10;
    public override int UpgradeId => 22;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalBugGold(10);
    }
}
