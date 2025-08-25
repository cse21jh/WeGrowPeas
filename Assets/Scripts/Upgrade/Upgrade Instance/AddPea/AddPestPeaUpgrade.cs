using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPestPeaUpgrade : Upgrade
{
    public override string Name => "해충 완두콩 추가";
    public override string Explanation => "해충에 강한 완두콩을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_3");
    public override int MaxAmount => -1;
    public override int UnlockStage => 10;
    public override int UpgradeId => 4;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.PestResistance, 0.5f, 1, GameManager.Instance.grid.GetAdditionalPestResistance())
        };
        GameManager.Instance.grid.AddPea(trait);
        Debug.Log(Explanation);
    }
}
