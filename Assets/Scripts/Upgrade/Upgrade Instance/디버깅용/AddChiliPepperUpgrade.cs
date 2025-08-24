using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddChiliPepperUpgrade : Upgrade
{
    public override string Name => "고추 추가";
    public override string Explanation => "고추를 12번 칸에 추가합니다(테스트용)";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/ChiliPepper/ChiliPepper");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 302;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddChiliPepper(12); 
    }
}
