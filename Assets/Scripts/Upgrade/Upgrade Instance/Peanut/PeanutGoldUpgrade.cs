using UnityEngine;

public class PeanutGoldUpgrade : Upgrade
{
    public override string Name => "¶¥Äá ¸À °³¼±";
    public override string Explanation => "¶¥ÄáÀÌ ÁÖ´Â °ñµå°¡ 10 Áõ°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => 3;
    public override int UnlockStage => 1;
    public override int UpgradeId => 30;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalPeanutGold(10);
    }
}
