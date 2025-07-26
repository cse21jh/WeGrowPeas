using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestResistenceUpgrade : Upgrade
{
    public override string Name => "���� ���� Ȯ�� ����";
    public override string Explanation => "���濡 ������ Ȯ���� 3% �����մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 5;
    public override int UnlockStage => 999999;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistance(CompleteTraitType.PestResistance, 0.03f);
        Debug.Log(Explanation);
    }
}
