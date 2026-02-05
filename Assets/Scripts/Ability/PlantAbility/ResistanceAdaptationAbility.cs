using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceAdaptation", menuName = "Abilities/Plant/Pea/ResistanceAdaptation")]
public class ResistanceAdaptationAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.AddResistanceAdaptation(0.01f * level);
    }
}
