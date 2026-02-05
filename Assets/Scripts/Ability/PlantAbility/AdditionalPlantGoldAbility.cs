using UnityEngine;

[CreateAssetMenu(fileName = "AdditionalPlantGold", menuName = "Abilities/Plant/Common/AdditionalPlantGold")]
public class AdditionalPlantGoldAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.AddAdditionalPlantGold(20 * level);
    }
}
