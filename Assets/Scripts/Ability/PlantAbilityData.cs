using UnityEngine;

[LocGroup(LocPrefix, "name", "description")]
public abstract class PlantAbilityData : ScriptableObject
{
    private const string LocPrefix = "ability";

    /// <summary>화면에 보여줄 이름. 저장과 비교에는 에셋 이름을 쓴다.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, name, "name"));

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), "");

    [Header("기본 정보")]
    public Sprite icon;
    [Header("식물 특성 공통 정보")]
    public PlayablePlantType type;
    public int level;

    public abstract void ApplyEffect(GameManager gameManager);
}
