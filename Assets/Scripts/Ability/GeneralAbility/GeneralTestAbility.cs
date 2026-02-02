using UnityEngine;

[CreateAssetMenu(fileName = "Test(테스트)", menuName = "Abilities/General/Test")]
public class GeneralTestAbility : GeneralAbilityData
{
    public override void ApplyEffect(GameManager gameManager)
    {
        Debug.Log("히히 일반 특성이당");
    }
}
