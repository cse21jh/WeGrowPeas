using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHeavyRainPlantUpgrade : Upgrade
{
    public override string Name => "폭우 식물 추가";
    public override string Explanation => "폭우에 강한 식물을 하나 추가합니다";
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
