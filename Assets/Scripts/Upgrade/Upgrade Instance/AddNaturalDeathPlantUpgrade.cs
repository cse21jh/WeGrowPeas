using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNaturalDeathPlantUpgrade : Upgrade
{
    public override string Name => "자연사 식물 추가";
    public override string Explanation => "자연사에 강한 식물을 하나 추가합니다";
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.8f, 0)
        };
        GameManager.Instance.grid.AddPlant(trait); 
        Debug.Log(Explanation);
    }
}
