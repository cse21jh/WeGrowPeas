using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>특수 아이템 목록. 이번 판의 보유와 선물은 <see cref="SpecialItemState"/>(<c>GameRun.Special</c>).</summary>
public static class SpecialItemSystem
{
    public const string ResourcePath = "Data/SpecialItem";

    /// <summary>선택지 한 칸당 사용할 수 있는 리롤 횟수.</summary>
    public const int RerollPerSlot = 1;

    /// <summary>선택지 칸 수(카드 개수).</summary>
    public const int SlotCount = 3;

    private static SpecialItemData[] _all;
    public static IReadOnlyList<SpecialItemData> All
    {
        get { if (_all == null) _all = Resources.LoadAll<SpecialItemData>(ResourcePath).Where(d => d != null).OrderBy(d => d.id, System.StringComparer.Ordinal).ToArray(); return _all; }
    }

    // ── 조회 ──────────────────────────────────────────────────────────────────
    public static SpecialItemData GetData(string id)
        => All.FirstOrDefault(d => d != null && d.id == id);

    /// <summary>
    /// 지정한 식물로 "새벽 N단계를 클리어하면" 새로 해금되는 식물별 특수 아이템 목록.
    /// (새벽 UI에서 단계별 해금 특수 아이템을 자동 표시하는 데 사용)
    /// </summary>
    public static List<SpecialItemData> GetItemsUnlockedAtStage(int stage, string plant)
    {
        if (stage <= 0) return new List<SpecialItemData>();
        return All.Where(d => d != null && d.plantSpecific
            && d.unlockDawnStage == stage && d.plantName == plant).ToList();
    }
}
