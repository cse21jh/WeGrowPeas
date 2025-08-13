using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNepenthesUpgrade : Upgrade
{
    public override string Name => "네펜데스 추가";
    public override string Explanation => "네펜데스를 12번 칸에 추가합니다(테스트용)";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Nepenthes/Nepenthes");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddNepenthes(12); 
    }
}
