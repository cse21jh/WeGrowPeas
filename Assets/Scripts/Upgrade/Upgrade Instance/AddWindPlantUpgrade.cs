using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddWindPlantUpgrade : Upgrade
{
    public override string Name => "�ٶ� �Ĺ� �߰�";
    public override string Explanation => "�ٶ��� ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_1");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1),
            new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
