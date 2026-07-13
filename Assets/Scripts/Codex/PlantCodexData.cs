using UnityEngine;

/// <summary>
/// 도감 "식물" 항목 데이터. 식물 클래스에 설명/tmi/특성 텍스트가 없어 별도로 둔다.
/// <see cref="plantId"/>는 Plant.speciesname과 일치 (예: 완두콩, 땅콩).
/// 유전자별 최대 저항력 표는 UI에서 코드/이 데이터로 구성.
/// </summary>
[CreateAssetMenu(menuName = "Codex/Plant Codex Data")]
public class PlantCodexData : ScriptableObject
{
    [Tooltip("Plant.speciesname과 일치 (예: 완두콩, 땅콩, 피스타치오)")]
    public string plantId;

    public string displayName;
    public Sprite icon;

    [TextArea]
    [Tooltip("특징 설명 및 tmi (예: 자가번식 가능, 칼로리 높음)")]
    public string description;

    [TextArea]
    [Tooltip("특성/특이사항 설명 (예: 네펜데스는 벌레 면역)")]
    public string traitInfo;

    [TextArea]
    [Tooltip("유전자별 최대 저항력 등 추가 설명(선택). 비우면 UI에서 코드값으로 표시 시도.")]
    public string resistanceNote;
}
