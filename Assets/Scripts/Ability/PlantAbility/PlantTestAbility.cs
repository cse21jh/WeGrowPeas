using UnityEngine;

[CreateAssetMenu(fileName = "Test(테스트)", menuName = "Abilities/Pea/Test")]
public class PlantTestAbility : PlantAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        Debug.Log("히히 식물 특성이당" + level.ToString());
    }
}
