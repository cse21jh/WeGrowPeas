using UnityEngine;

public class PeanutCopyUpgrade : Upgrade
{
    public override string Name => "¶¥Äá ÀÚ°¡¹ø½Ä È®·ü Áõ°¡";
    public override string Explanation => "¶¥ÄáÀÌ ÀÚ°¡¹ø½Ä ÇÒ È®·üÀÌ 2% Áõ°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => 5;
    public override int UnlockStage => 11;
    public override int UpgradeId => 29;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalPeanutCopyProbability(0.02f);
    }
}
