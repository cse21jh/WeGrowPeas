using UnityEngine;

[CreateAssetMenu(fileName = "PlantInfoData", menuName = "Data/PlantInfoData")]
[LocGroup(LocPrefix, "name", "description")]
public class PlantInfoData : ScriptableObject
{
    private const string LocPrefix = "plantinfo";

    /// <summary>화면에 보여줄 이름. 저장 키나 비교에는 <see cref="plantName"/>를 쓴다.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, name, "name"));

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), "");

    public PlayablePlantType type;
    [Tooltip("식물 종 식별자(PlantIds). 저장과 비교에 쓴다. 표시 이름은 번역 표에서 고친다.")]
    public string plantName;
    public Sprite icon;
    public int price;
}
