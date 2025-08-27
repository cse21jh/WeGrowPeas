using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddWindPeanutUpgrade : Upgrade
{
    public override string Name => "¹Ù¶÷ ¶¥Äá Ãß°¡";
    public override string Explanation => "¹Ù¶÷¿¡ °­ÇÑ ¶¥ÄáÀ» ÇÏ³ª Ãß°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 24;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.4f, 1, 0.0f),
            new GeneticTrait(CompleteTraitType.WindResistance, 0.4f, 1, 0.0f)
        };
        GameManager.Instance.grid.AddPeanut(trait);
        Debug.Log(Explanation);
    }
}
