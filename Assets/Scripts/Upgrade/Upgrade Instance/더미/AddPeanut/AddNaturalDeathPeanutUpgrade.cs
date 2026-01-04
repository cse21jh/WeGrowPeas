using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNaturalDeathPeanutUpgrade : Upgrade
{
    public override string Name => "자연사 땅콩 추가";
    public override string Explanation => "자연사에 강한 땅콩을 둘 추가합니다";
    public override Sprite Icon => Resources.Load<Sprite>("Sprites/Plant/Peanut/Peanut");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 23;
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
