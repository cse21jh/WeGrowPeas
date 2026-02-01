using UnityEngine;

public abstract class PlantAbilityData : ScriptableObject
{
    [Header("기본 정보")]
    public string abilityName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    [Header("식물 특성 공통 정보")]
    public AbilityType type;
    public int level;

    public enum AbilityType
    {
        PlantAbility, // 공통으로 사용되는 특성. 아래는 식물별 고유 특성
        PeaAbility,
        PeanutAbility,
    }

    public abstract void ApplyEffect(GameManager gameManager);
}
