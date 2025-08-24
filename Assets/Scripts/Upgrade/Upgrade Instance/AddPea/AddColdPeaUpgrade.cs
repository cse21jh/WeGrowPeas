using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddColdPeaUpgrade : Upgrade
{
    public override string Name => "추위 식물 추가";
    public override string Explanation => "추위에 강한 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_4");
    public override int MaxAmount => -1;
    public override int UnlockStage => 15;
    public override int UpgradeId => 5;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.ColdResistance, 0.5f , 1, 0.0f)
        };
        GameManager.Instance.grid.AddPea(trait);
        Debug.Log(Explanation);
    }
}
