using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdResistenceUpgrade : Upgrade
{
    public override string Name => "���� ���� Ȯ�� ����";
    public override string Explanation => "������ �Ĺ��� ������ ������ Ȯ���� 5% �����մϴ� (�ִ� 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_10");
    public override int MaxAmount => 5;
    public override int UnlockStage => 20;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.ColdResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
