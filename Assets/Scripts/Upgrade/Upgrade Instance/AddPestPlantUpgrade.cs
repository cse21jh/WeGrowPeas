using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPestPlantUpgrade : Upgrade
{
    public override string Name => "해충 식물 추가";
    public override string Explanation => "해충에 강한 식물을 하나 추가합니다";
    public override int MaxAmount => -1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.PestResistance, 0.9f, 2)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
