using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// "이번 판에 새로 해금된 아이템"을 판정하기 위한 런 단위 추적기.
///
/// 런 시작 시 <see cref="CaptureRunStart"/>로 그 시점의 메타 해금 상태를 스냅샷으로 남기고,
/// 결과창에서 <see cref="GetNewlyUnlocked"/>로 그 사이에 새로 열린 아이템을 뽑는다.
/// (새벽 단계 클리어 해금 · 인게임 사건 해금 양쪽 모두 커버)
///
/// static이라 씬 전환(농장 → GameOverScene)을 넘어 유지되며, 앱 재시작 시 초기화된다.
/// </summary>
public static class UnlockRunTracker
{
    /// <summary>ItemData 에셋이 모여 있는 Resources 경로. (CodexCatalog와 동일)</summary>
    public const string ItemResourcePath = "Data/Item Data";

    // 런 시작 시점에 이미 메타 해금돼 있던 아이템의 UnlockId
    private static HashSet<string> runStartUnlocked;

    /// <summary>스냅샷이 찍혀 있는가. (찍히지 않았으면 새로 해금된 것을 판정할 수 없다)</summary>
    public static bool HasSnapshot => runStartUnlocked != null;

    /// <summary>런 시작 시 호출. 현재 메타 해금 상태를 스냅샷으로 남긴다.</summary>
    public static void CaptureRunStart()
    {
        runStartUnlocked = new HashSet<string>();
        foreach (var item in LoadAllItems())
        {
            if (item.IsMetaUnlocked())
                runStartUnlocked.Add(item.UnlockId);
        }
        Debug.Log($"[UnlockRunTracker] 런 시작 스냅샷: 해금 {runStartUnlocked.Count}개");
    }

    /// <summary>
    /// 런 시작 이후 새로 메타 해금된 아이템 목록. 스냅샷이 없으면 빈 목록.
    /// </summary>
    public static List<ItemData> GetNewlyUnlocked()
    {
        if (runStartUnlocked == null)
            return new List<ItemData>();

        return LoadAllItems()
            .Where(item => item.IsMetaUnlocked() && !runStartUnlocked.Contains(item.UnlockId))
            .ToList();
    }

    /// <summary>스냅샷 폐기(새 런 시작 전이나 테스트용).</summary>
    public static void Clear()
    {
        runStartUnlocked = null;
    }

    /// <summary>
    /// 지정한 식물로 "새벽 N단계를 클리어하면" 새로 해금되는 상점 아이템 목록.
    /// (새벽 UI에서 단계별 해금 아이템을 자동 표시하는 데 사용)
    /// 식물 조건이 없는 아이템(무당벌레·페트병 등)은 어느 식물이든 포함된다.
    /// </summary>
    public static List<ItemData> GetItemsUnlockedAtStage(int stage, string plant)
    {
        if (stage <= 0) return new List<ItemData>();

        return LoadAllItems()
            .Where(item => item.metaRequiredDawnStage == stage
                && (string.IsNullOrEmpty(item.metaRequiredDawnPlant) || item.metaRequiredDawnPlant == plant))
            .ToList();
    }

    private static IEnumerable<ItemData> LoadAllItems()
    {
        return Resources.LoadAll<ItemData>(ItemResourcePath).Where(i => i != null);
    }
}
