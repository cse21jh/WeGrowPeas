using System.Linq;
using UnityEngine;

/// <summary>
/// 해금 "부여"(기록) 로직. 조건이 충족되는 순간 UnlockManager에 아이템의 UnlockId를 기록한다.
///
/// 설계: 해금 여부 판정은 항상 UnlockManager.IsUnlocked(UnlockId) 한 곳으로 통일하고,
/// "어떤 조건으로 해금되는가(트리거)"만 여기서 다양하게 처리한다.
/// - 새벽 단계 클리어 → <see cref="GrantDawnClearUnlocks"/> (40일 클리어 시 호출)
/// - 인게임 사건 → <see cref="GrantEventUnlocks"/> (겨울 도달 등에서 호출)
/// 나중에 "특정 조건" 해금 아이템이 생기면, 그 조건 트리거에서 UnlockManager.Unlock(UnlockId)만 부르면 된다.
/// </summary>
public static class UnlockGrants
{
    /// <summary>
    /// 지정한 식물로 새벽 clearedStage 단계까지 클리어했을 때, 그 조건을 만족하는
    /// 상점/특수 아이템을 실제로 해금(UnlockManager 기록)한다. (DawnSystem.RecordRunCleared에서 호출)
    /// </summary>
    public static void GrantDawnClearUnlocks(string plant, int clearedStage)
    {
        if (clearedStage <= 0) return;

        // 상점 아이템: metaRequiredDawnStage <= clearedStage, 식물 조건 일치(없으면 어느 식물이든)
        foreach (var it in Resources.LoadAll<ItemData>(UnlockRunTracker.ItemResourcePath))
        {
            if (it == null) continue;
            if (it.metaRequiredDawnStage <= 0 || it.metaRequiredDawnStage > clearedStage) continue;
            if (!string.IsNullOrEmpty(it.metaRequiredDawnPlant) && it.metaRequiredDawnPlant != plant) continue;
            UnlockManager.Unlock(it.UnlockId);
        }

        // 특수 아이템: 그 식물 전용, unlockDawnStage <= clearedStage
        foreach (var sp in Resources.LoadAll<SpecialItemData>(SpecialItemSystem.ResourcePath))
        {
            if (sp == null || !sp.plantSpecific) continue;
            if (sp.unlockDawnStage <= 0 || sp.unlockDawnStage > clearedStage) continue;
            if (sp.plantName != plant) continue;
            UnlockManager.Unlock(sp.UnlockId);
        }

        Debug.Log($"[UnlockGrants] {plant} 새벽 {clearedStage}단계 클리어 → 조건 만족 아이템 해금 기록");
    }

    /// <summary>
    /// 인게임 사건(황금 식물 제작·겨울 도달 등) 발생 시, 그 사건을 조건으로 하는 상점 아이템을 해금한다.
    /// eventId는 UnlockManager.Ids 참고.
    /// </summary>
    public static void GrantEventUnlocks(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;

        // 사건 id 자체도 기록(하위 호환/디버그 표시용)
        UnlockManager.Unlock(eventId);

        foreach (var it in Resources.LoadAll<ItemData>(UnlockRunTracker.ItemResourcePath))
        {
            if (it == null) continue;
            if (it.metaRequiredEventId == eventId)
                UnlockManager.Unlock(it.UnlockId);
        }
    }
}
