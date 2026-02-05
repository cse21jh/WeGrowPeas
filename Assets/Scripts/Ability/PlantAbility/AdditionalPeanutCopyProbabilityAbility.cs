using UnityEngine;

[CreateAssetMenu(fileName = "AdditionalPeanutCopyProbability", menuName = "Abilities/Plant/Peanut/AdditionalPeanutCopyProbability")]
public class AdditionalPeanutCopyProbabilityAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.AddAdditionalPeanutCopyProbability(0.02f * level);
    }
}
