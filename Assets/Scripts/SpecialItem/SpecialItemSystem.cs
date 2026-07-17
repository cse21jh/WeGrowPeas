using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 특수 아이템 런타임 상태(정적 접근자, CurseState/DawnSystem 패턴).
/// - 보유: 이번 런에 획득한 아이템 id 집합. 효과 코드가 <see cref="Has"/>로 조회.
/// - 미수령 선물: 10·20·30일 자유시간에 +1, 수령(3택 선택) 시 -1. 수령 전까지 계속 유지.
/// - 저장: 보유 id + 선물 수를 SaveData에 넣고 로드 시 복원. 새 게임 시 <see cref="ResetRun"/>.
/// </summary>
public static class SpecialItemSystem
{
    public const string ResourcePath = "Data/SpecialItem";

    private static readonly HashSet<string> _owned = new HashSet<string>();
    public static int PendingGifts { get; private set; }

    private static SpecialItemData[] _all;
    private static SpecialItemData[] All
    {
        get { if (_all == null) _all = Resources.LoadAll<SpecialItemData>(ResourcePath); return _all; }
    }

    // ── 조회 ──────────────────────────────────────────────────────────────────
    public static bool Has(string id) => !string.IsNullOrEmpty(id) && _owned.Contains(id);
    public static IReadOnlyCollection<string> OwnedIds => _owned;

    public static SpecialItemData GetData(string id)
        => All.FirstOrDefault(d => d != null && d.id == id);

    // ── 선물 지급/수령 ─────────────────────────────────────────────────────────
    /// <summary>10·20·30일 자유시간 시작 시 호출 — 선물 +1 (수령 전까지 유지·누적).</summary>
    public static void AddGift()
    {
        PendingGifts++;
        Debug.Log($"[SpecialItem] 선물 도착 (미수령 {PendingGifts}개)");
    }

    /// <summary>선택 후보 3개 롤. 공용 전체 + (현재 식물 + 언락된) 식물별 − 이미 보유.</summary>
    public static List<SpecialItemData> RollCandidates(string currentPlant, int count = 3)
    {
        var pool = new List<SpecialItemData>();
        foreach (var d in All)
        {
            if (d == null || string.IsNullOrEmpty(d.id) || _owned.Contains(d.id)) continue;
            if (d.plantSpecific)
            {
                if (d.plantName != currentPlant) continue;
                if (!UnlockManager.IsUnlocked(d.UnlockId)) continue;
            }
            pool.Add(d);
        }

        // 셔플 후 앞에서 count개
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.GetRange(0, Mathf.Min(count, pool.Count));
    }

    /// <summary>3택에서 하나 선택 — 보유 등록 + 선물 1개 소모.</summary>
    public static void Acquire(SpecialItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        _owned.Add(item.id);
        PendingGifts = Mathf.Max(0, PendingGifts - 1);
        Debug.Log($"[SpecialItem] 획득: {item.displayName} ({item.id})");
    }

    // ── 저장/로드 (per-run) ────────────────────────────────────────────────────
    public static List<string> GetSaveOwned() => new List<string>(_owned);
    public static int GetSavePending() => PendingGifts;

    public static void LoadFromSave(List<string> owned, int pending)
    {
        _owned.Clear();
        if (owned != null) foreach (var id in owned) if (!string.IsNullOrEmpty(id)) _owned.Add(id);
        PendingGifts = Mathf.Max(0, pending);
    }

    /// <summary>새 게임 시작 시 초기화.</summary>
    public static void ResetRun()
    {
        _owned.Clear();
        PendingGifts = 0;
    }
}
