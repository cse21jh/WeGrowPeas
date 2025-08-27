using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddColdPeanutUpgrade : Upgrade
{
    public override string Name => "ÃßÀ§ ¶¥Äá Ãß°¡";
    public override string Explanation => "ÃßÀ§¿¡ °­ÇÑ ¶¥ÄáÀ» ÇÏ³ª Ãß°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => -1;
    public override int UnlockStage => 15;
    public override int UpgradeId => 27;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.ColdResistance, 0.4f , 1, 0.0f)
        };
        GameManager.Instance.grid.AddPeanut(trait);
        Debug.Log(Explanation);
    }
}
