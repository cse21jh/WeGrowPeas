using UnityEngine;

/// <summary>
/// 도감 "식물" 항목 데이터. 식물 클래스에 설명/tmi/특성 텍스트가 없어 별도로 둔다.
/// <see cref="plantId"/>는 Plant.speciesname과 일치 (예: 완두콩, 땅콩).
/// 유전자별 최대 저항력 표는 UI에서 코드/이 데이터로 구성.
/// </summary>
[CreateAssetMenu(menuName = "Codex/Plant Codex Data")]
[LocGroup(LocPrefix, "name", "description")]
public class PlantCodexData : ScriptableObject, ILocKeyed
{
    private const string LocPrefix = "plant";

    /// <summary>화면에 보여줄 이름.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, LocId, "name"));

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, LocId, "description"), "");

    /// <summary>화면에 보여줄 특성 설명.</summary>
    public string LocalizedTraitInfo => Loc.Or(LocKey.For(LocPrefix, LocId, "trait"), "");

    /// <summary>화면에 보여줄 저항력 설명.</summary>
    public string LocalizedResistanceNote => Loc.Or(LocKey.For(LocPrefix, LocId, "resistance"), "");

    /// <inheritdoc/>
    public string LocId => string.IsNullOrEmpty(locId) ? name : locId;

    [Tooltip("번역 키에 쓰는 영문 id. 파일 이름이 한글이라 따로 둔다.")]
    [SerializeField] private string locId;

    [Tooltip("Plant.speciesname과 일치 (예: 완두콩, 땅콩, 피스타치오)")]
    public string plantId;

    public Sprite icon;
}
