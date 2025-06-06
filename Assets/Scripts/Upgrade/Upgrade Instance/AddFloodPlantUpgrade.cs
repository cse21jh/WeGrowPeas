using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddFloodPlantUpgrade : Upgrade
{
    public override string Name => "홍수 식물 추가";
    public override string Explanation => "홍수에 강한 식물을 하나 추가합니다";
    public override int MaxAmount => -1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.FloodResistance, 0.9f, 2)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
