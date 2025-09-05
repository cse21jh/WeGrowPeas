using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHeavyRainPlantUpgrade : Upgrade
{
    public override string Name => "폭우 식물 추가";
    public override string Explanation => "폭우에 강한 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_5");
    public override int MaxAmount => -1;
    public override int UnlockStage => HeavyRainWave.UnlockStage - 5;
    public override int UpgradeId => 6;
    public override void OnSelectAction()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.HeavyRainResistance, 0.5f , 1, 0.0f)
        };
        List<GeneticTrait> peanutTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.HeavyRainResistance, 0.4f , 1, 0.0f)
        };
        GameManager.Instance.upgradeManager.addPeaTrait = peaTrait;
        GameManager.Instance.upgradeManager.addPeanutTrait = peanutTrait;
        Debug.Log(Explanation);
    }
}
