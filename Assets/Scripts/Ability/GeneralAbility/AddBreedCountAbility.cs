using UnityEngine;

[CreateAssetMenu(fileName = "AddBreedCountAbility", menuName = "Abilities/General/AddBreedCountAbility")]
public class AddBreedCountAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.AddMaxBreedCount(1);
    }
}
