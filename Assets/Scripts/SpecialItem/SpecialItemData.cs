using UnityEngine;

/// <summary>
/// 특수 아이템 1종의 정적 데이터.
/// 10·20·30일 자유시간에 도착하는 선물에서 3택 1로 획득. 공용 / 식물별(새벽 클리어 언락)로 나뉜다.
/// </summary>
[CreateAssetMenu(menuName = "SpecialItem/Special Item Data")]
public class SpecialItemData : ScriptableObject
{
    [Tooltip("고유 id (효과 코드에서 SpecialItemState.Has(id)로 조회)")]
    public string id;

    public string displayName;
    public Sprite icon;

    [TextArea] public string description;

    [Header("등장 조건")]
    [Tooltip("false = 공용(항상 후보), true = 식물별(해당 식물 + 언락 필요)")]
    public bool plantSpecific = false;

    [Tooltip("plantSpecific일 때 대상 식물 이름 (GameManager.currentPlant와 비교: 완두콩/땅콩)")]
    public string plantName;

    [Tooltip("plantSpecific일 때 해금에 필요한 새벽 클리어 단계 (4/8/12)")]
    public int unlockDawnStage;

    /// <summary>식물별 아이템의 언락 id (UnlockManager 재활용).</summary>
    public string UnlockId => $"special_{id}";
}
