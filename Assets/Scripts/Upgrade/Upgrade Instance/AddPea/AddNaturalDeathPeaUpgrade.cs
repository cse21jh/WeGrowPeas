using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNaturalDeathPeaUpgrade : Upgrade
{
    public override string Name => "자연사 완두콩 추가";
    public override string Explanation => "자연사에 강한 완두콩을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_0");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.8f , 2, 0.0f)
        };
        GameManager.Instance.grid.AddPea(trait);
        Debug.Log(Explanation);
    }
}
