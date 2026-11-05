using UnityEngine;

[LocGroup(LocPrefix)]
public abstract class GeneralAbilityData : ScriptableObject
{
    private const string LocPrefix = "ability";

    /// <summary>해금 여부의 저장 키. 에셋 이름을 바꾸면 <see cref="LegacySaveKeys"/>도 함께 고친다.</summary>
    public string UnlockKey => name;

    /// <summary>화면에 보여줄 이름. 저장 키가 필요하면 <see cref="UnlockKey"/>를 쓴다.</summary>
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
