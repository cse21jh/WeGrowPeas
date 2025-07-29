using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindResistenceUpgrade : Upgrade
{
    public override string Name => "�ٶ� ���� Ȯ�� ����";
    public override string Explanation => "������ �Ĺ��� �ٶ��� ������ Ȯ���� 5% �����մϴ� (�ִ� 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_7");
    public override int MaxAmount => 5;
    public override int UnlockStage => 5;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.WindResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
