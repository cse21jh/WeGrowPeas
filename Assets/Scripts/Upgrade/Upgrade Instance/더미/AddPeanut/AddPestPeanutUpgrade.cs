using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddPestPeanutUpgrade : Upgrade
{
    public override string Name => "ÇØÃæ ¶¥Äá Ãß°¡";
    public override string Explanation => "ÇØÃæ¿¡ °­ÇÑ ¶¥ÄáÀ» ÇÏ³ª Ãß°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => -1;
    public override int UnlockStage => 10;
    public override int UpgradeId => 26;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(TraitType.Pest, 0.4f , 1, GameManager.Instance.grid.GetAdditionalPestResistance())
        };
        GameManager.Instance.grid.AddPeanut(trait);
        Debug.Log(Explanation);
    }
}
