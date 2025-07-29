using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodResistenceUpgrade : Upgrade
{
    public override string Name => "ȫ�� ���� Ȯ�� ����";
    public override string Explanation => "������ �Ĺ��� ȫ���� ������ Ȯ���� 5% �����մϴ� (�ִ� 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_8");
    public override int MaxAmount => 5;
    public override int UnlockStage => 10;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.FloodResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
