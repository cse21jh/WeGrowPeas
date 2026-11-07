using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceBonus", menuName = "Abilities/Plant/Common/ResistanceBonus")]
public class ResistanceBonusAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.Upgrades.AddResistanceBonus(0.02f * level);
    }
}
