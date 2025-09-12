using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddFloodPlantUpgrade : Upgrade
{
    public override string Name => "홍수 식물 추가";
    public override string Explanation => "홍수에 강한 식물을 하나 추가합니다";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_2");
    public override int MaxAmount => -1;
    public override int UnlockStage => FloodWave.UnlockStage - 5;
    public override int UpgradeId => 3;
    public override void OnSelectAction()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.FloodResistance, 0.5f , 1, 0.0f)
        };
        /*
        List<GeneticTrait> peanutTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.4f , 1, 0.0f),
            new GeneticTrait(CompleteTraitType.FloodResistance, 0.4f , 1, 0.0f)
        };
        GameManager.Instance.upgradeManager.addPeaTrait = peaTrait;
        GameManager.Instance.upgradeManager.addPeanutTrait = peanutTrait;
        */ // 땅콩단은 복귀할 것입니다
        GameManager.Instance.grid.AddPea(peaTrait);
        Debug.Log(Explanation);
    }
}
