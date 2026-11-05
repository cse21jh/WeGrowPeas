using UnityEngine;

[LocGroup(LocPrefix, "name", "description")]
public abstract class GeneralAbilityData : ScriptableObject
{
    private const string LocPrefix = "ability";

    /// <summary>해금 여부의 저장 키. 에셋 이름을 바꾸면 <see cref="LegacySaveKeys"/>도 함께 고친다.</summary>
    public string UnlockKey => name;

    /// <summary>화면에 보여줄 이름. 저장 키가 필요하면 <see cref="UnlockKey"/>를 쓴다.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, name, "name"));

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), "");

    [Header("기본 정보")]
    public Sprite icon;
    public int price;
    public abstract void ApplyEffect(GameManager gameManager);
}
