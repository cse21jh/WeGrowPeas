using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestResistenceUpgrade : Upgrade
{
    public override string Name => "���� ���� Ȯ�� ����";
    public override string Explanation => "������ �Ĺ��� ���濡 ������ Ȯ���� 5% �����մϴ� (�ִ� 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_9");
    public override int MaxAmount => 5;
    public override int UnlockStage => 999999;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.PestResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
