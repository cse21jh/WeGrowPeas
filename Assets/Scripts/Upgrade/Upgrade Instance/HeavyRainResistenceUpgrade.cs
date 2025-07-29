using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainResistenceUpgrade : Upgrade
{
    public override string Name => "���� ���� Ȯ�� ����";
    public override string Explanation => "������ �Ĺ��� ���쿡 ������ Ȯ���� 5% �����մϴ� (�ִ� 15%)";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_11");
    public override int MaxAmount => -1;
    public override int UnlockStage => 25;
    public override void OnSelectAction()
    {
        GameManager.Instance.grid.AddAdditionalResistanceInGrid(CompleteTraitType.HeavyRainResistance, 0.05f);
        Debug.Log(Explanation);
    }
}
