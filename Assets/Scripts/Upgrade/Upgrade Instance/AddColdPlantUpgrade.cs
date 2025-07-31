using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddColdPlantUpgrade : Upgrade
{
    public override string Name => "���� �Ĺ� �߰�";
    public override string Explanation => "������ ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_4");
    public override int MaxAmount => -1;
    public override int UnlockStage => 15;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.ColdResistance, 0.5f , 1, 0.0f)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
