using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddWindPlantUpgrade : Upgrade
{
    public override string Name => "바람 식물 추가";
    public override string Explanation => "바람에 강한 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_1");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
            new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
        };
        GameManager.Instance.grid.AddPlant(trait);
        Debug.Log(Explanation);
    }
}
