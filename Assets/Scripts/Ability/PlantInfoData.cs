using UnityEngine;

[CreateAssetMenu(fileName = "PlantInfoData", menuName = "Data/PlantInfoData")]
[LocGroup(LocPrefix)]
public class PlantInfoData : ScriptableObject
{
    private const string LocPrefix = "plantinfo";

    /// <summary>화면에 보여줄 이름. 저장 키나 비교에는 <see cref="plantName"/>를 쓴다.</summary>
    public string LocalizedName => Loc.Or(LocKey.For(LocPrefix, name, "name"), plantName);

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), description);

    public PlayablePlantType type;
    [LocText("name")] public string plantName;
    [TextArea, LocText("description")] public string description;
    public Sprite icon;
    public int price;
}
