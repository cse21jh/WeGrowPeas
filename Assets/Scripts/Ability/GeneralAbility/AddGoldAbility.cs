using UnityEngine;

[CreateAssetMenu(fileName = "AddGoldAbility", menuName = "Abilities/General/AddGoldAbility")]
public class AddGoldAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.economyManager.AddGold(1000);
    }
}
