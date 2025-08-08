using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNepenthesUpgrade : Upgrade
{
    public override string Name => "���浥�� �߰�";
    public override string Explanation => "���浥���� 12�� ĭ�� �߰��մϴ�(�׽�Ʈ��)";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Nepenthes/Nepenthes");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddNepenthes(12); 
    }
}
