using UnityEngine;

[LocGroup(LocPrefix)]
public abstract class GeneralAbilityData : ScriptableObject
{
    private const string LocPrefix = "ability";

    /// <summary>화면에 보여줄 이름. 저장 키나 비교에는 <see cref="abilityName"/>를 쓴다.</summary>
    public string LocalizedName => Loc.Or(LocKey.For(LocPrefix, name, "name"), abilityName);

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), description);

    [Header("기본 정보")]
    [LocText("name")] public string abilityName;
    [TextArea(3, 5), LocText("description")]
    public string description;
    public Sprite icon;
    public int price;
    public abstract void ApplyEffect(GameManager gameManager);
}
