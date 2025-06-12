using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddSoilUpgrade : Upgrade
{
    public override string Name => "³óÀå È®Àå";
    public override string Explanation => "³óÀåÀÌ 4Ä­ Áõ°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_12");
    public override int MaxAmount => 4;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddSoil();
    }
}
