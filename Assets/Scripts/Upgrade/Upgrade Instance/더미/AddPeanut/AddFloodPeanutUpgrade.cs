using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddFloodPeanutUpgrade : Upgrade
{
    public override string Name => "È«¼ö ¶¥Äá Ãß°¡";
    public override string Explanation => "È«¼ö¿¡ °­ÇÑ ¶¥ÄáÀ» ÇÏ³ª Ãß°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => -1;
    public override int UnlockStage => 5;
    public override int UpgradeId => 25;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(TraitType.Flood, 0.4f , 1, 0.0f)
        };
        GameManager.Instance.grid.AddPeanut(trait);
        Debug.Log(Explanation);
    }
}
