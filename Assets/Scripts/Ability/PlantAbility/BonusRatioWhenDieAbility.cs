using UnityEngine;

[CreateAssetMenu(fileName = "BonusRatioWhenDie", menuName = "Abilities/Plant/Peanut/BonusRatioWhenDie")]
public class BonusRatioWhenDieAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.AddBonusRatioWhenDie(0.05f * level);
    }
}
