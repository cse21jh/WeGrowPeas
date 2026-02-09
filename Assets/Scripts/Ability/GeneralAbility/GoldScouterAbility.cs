using UnityEngine;

[CreateAssetMenu(fileName = "GoldScouterAbility", menuName = "Abilities/General/GoldScouterAbility")]
public class GoldScouterAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.SetGoldScouter(true);
    }
}
