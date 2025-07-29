using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPestPlantUpgrade : Upgrade
{
    public override string Name => "���� �Ĺ� �߰�";
    public override string Explanation => "���濡 ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_3");
    public override int MaxAmount => -1;
    public override int UnlockStage => 10;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1),
            new GeneticTrait(CompleteTraitType.PestResistance, 0.5f + GameManager.Instance.grid.GetAdditionalPestResistance(), 1)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
