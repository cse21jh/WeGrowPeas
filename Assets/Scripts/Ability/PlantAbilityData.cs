using UnityEngine;

public abstract class PlantAbilityData : ScriptableObject
{
    [Header("기본 정보")]
    public string abilityName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    [Header("식물 특성 공통 정보")]
    public PlayablePlantType type;
    public int level;

    public abstract void ApplyEffect(GameManager gameManager);
}
