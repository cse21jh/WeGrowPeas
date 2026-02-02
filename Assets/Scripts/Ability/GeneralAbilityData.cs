using UnityEngine;

public abstract class GeneralAbilityData : ScriptableObject
{
    [Header("기본 정보")]
    public string abilityName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public bool isUnlocked;
    public abstract void ApplyEffect(GameManager gameManager);
}
