using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddBasicPeanutUpgrade : Upgrade
{
    public override string Name => "±âº» ¶¥Äá 2°³ Ãß°¡";
    public override string Explanation => "±âº» ¶¥ÄáÀ» µÑ Ãß°¡ÇÕ´Ï´Ù";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => 1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 31;
    public override void OnSelectAction()
    {
        for (int i = 0; i < 2; i++)
        {
            List<GeneticTrait> trait = new List<GeneticTrait>
            {
            new GeneticTrait(TraitType.NaturalDeath, 0.4f , 1, 0.0f)
            };
            GameManager.Instance.grid.AddPeanut(trait);
        }
        Debug.Log(Explanation);
    }
}
