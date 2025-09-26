using UnityEngine;

public class BugGoldUpgrade : Upgrade
{
    public override string Name => "¹ú·¹°¡ ÁÖ´Â µ· Áõ°¡";
    public override string Explanation => "¹ú·¹°¡ ÁÖ´Â °ñµå°¡ 50 Áõ°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_25");
    public override int MaxAmount => 4;
    public override int UnlockStage => 11;
    public override int UpgradeId => 22;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalBugGold(50);
    }
}
