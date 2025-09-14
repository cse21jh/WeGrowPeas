using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNaturalDeathPlantUpgrade : Upgrade
{
    public override string Name => "자연사 식물 추가";
    public override string Explanation => "자연사에 강한 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("upgradeIconsSheet_0");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override int UpgradeId => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.8f , 2, 0.0f),
        };
        /*
        List<GeneticTrait> peanutTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.7f , 2, 0.0f),
        };
        GameManager.Instance.upgradeManager.addPeaTrait = peaTrait;
        GameManager.Instance.upgradeManager.addPeanutTrait = peanutTrait;
        */ // 땅콩단은 복귀할 것입니다
        GameManager.Instance.grid.AddPea(peaTrait);
        Debug.Log(Explanation);
    }
}
