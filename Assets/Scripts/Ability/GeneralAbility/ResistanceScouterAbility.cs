using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceScouterAbility", menuName = "Abilities/General/ResistanceScouterAbility")]
public class ResistanceScouterAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        gameManager.grid.SetResistanceScouter(true);
    }
}
