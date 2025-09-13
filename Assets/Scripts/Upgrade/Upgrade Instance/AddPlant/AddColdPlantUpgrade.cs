using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddColdPlantUpgrade : Upgrade
{
    public override string Name => "추위 식물 추가";
    public override string Explanation => "추위 저항력이 있는 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_4");
    public override int MaxAmount => -1;
    public override int UnlockStage => ColdWave.UnlockStage - 5;
    public override int UpgradeId => 5;
    public override void OnSelectAction()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.ColdResistance, 0.5f , 1, 0.0f)
        };
        /*
        List<GeneticTrait> peanutTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.ColdResistance, 0.4f , 1, 0.0f)
        };
        GameManager.Instance.upgradeManager.addPeaTrait = peaTrait;
        GameManager.Instance.upgradeManager.addPeanutTrait = peanutTrait;
        */ // 땅콩단은 복귀할 것입니다
        GameManager.Instance.grid.AddPea(peaTrait);
        Debug.Log(Explanation);
    }
}
