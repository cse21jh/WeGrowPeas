using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHeavyRainPlantUpgrade : Upgrade
{
    public override string Name => "���� �Ĺ� �߰�";
    public override string Explanation => "���쿡 ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_5");
    public override int MaxAmount => -1;
    public override int UnlockStage => 20;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1),
            new GeneticTrait(CompleteTraitType.HeavyRainResistance, 0.5f, 1)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
