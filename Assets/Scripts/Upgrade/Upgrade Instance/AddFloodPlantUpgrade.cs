using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddFloodPlantUpgrade : Upgrade
{
    public override string Name => "ȫ�� �Ĺ� �߰�";
    public override string Explanation => "ȫ���� ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_2");
    public override int MaxAmount => -1;
    public override int UnlockStage => 5;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f + GameManager.Instance.grid.GetAdditionalResistance(CompleteTraitType.NaturalDeath), 1),
            new GeneticTrait(CompleteTraitType.FloodResistance, 0.5f + GameManager.Instance.grid.GetAdditionalResistance(CompleteTraitType.FloodResistance), 1)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
