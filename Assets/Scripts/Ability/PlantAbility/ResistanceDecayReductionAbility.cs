using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceDecayReduction", menuName = "Abilities/Plant/Pea/ResistanceDecayReduction")]
public class ResistanceDecayReductionAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.Modifiers.AddResistanceDecayReduction(0.01f * level);
    }
}
