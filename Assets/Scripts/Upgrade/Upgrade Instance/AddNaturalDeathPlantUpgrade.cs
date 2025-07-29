using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddNaturalDeathPlantUpgrade : Upgrade
{
    public override string Name => "�ڿ��� �Ĺ� �߰�";
    public override string Explanation => "�ڿ��翡 ���� �Ĺ��� �ϳ� �߰��մϴ�";
    public override Sprite Icon => ResourceLoader.LoadUpgradeIcon("UpgradeIcons_0");
    public override int MaxAmount => -1;
    public override int UnlockStage => 1;
    public override void OnSelectAction()
    {
        List<GeneticTrait> trait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.8f , 0)
        };
        GameManager.Instance.grid.AddPlant(trait); 
        Debug.Log(Explanation);
    }
}
