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

    /// <summary>이번 런에 획득한 특수 아이템의 데이터 목록. (정보 앱 표시용)</summary>
    public static List<SpecialItemData> GetOwnedItems()
    {
        var list = new List<SpecialItemData>();
        foreach (var id in _owned)
        {
            var d = GetData(id);
            if (d != null) list.Add(d);
        }
        return list;
    }

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

    // ── 리롤 ──────────────────────────────────────────────────────────────────
    /// <summary>선택지 한 칸당 사용할 수 있는 리롤 횟수.</summary>
    public const int RerollPerSlot = 1;

    /// <summary>선택지 칸 수(카드 개수).</summary>
    public const int SlotCount = 3;

    // 칸별 남은 리롤 횟수
    private static readonly int[] _slotRerolls = CreateSlotRerolls();

    private static int[] CreateSlotRerolls()
    {
        var arr = new int[SlotCount];
        for (int i = 0; i < arr.Length; i++) arr[i] = RerollPerSlot;
        return arr;
    }

    /// <summary>해당 칸의 남은 리롤 횟수.</summary>
    public static int GetSlotRerolls(int slot)
        => (slot >= 0 && slot < _slotRerolls.Length) ? _slotRerolls[slot] : 0;

    public static bool CanRerollSlot(int slot) => GetSlotRerolls(slot) > 0;

    /// <summary>해당 칸의 리롤 1회 소모. 성공하면 true.</summary>
    public static bool UseSlotReroll(int slot)
    {
        if (!CanRerollSlot(slot)) return false;
        _slotRerolls[slot]--;
        return true;
    }

    /// <summary>모든 칸의 리롤 횟수 회복 (새 선물).</summary>
    private static void ResetSlotRerolls()
    {
        for (int i = 0; i < _slotRerolls.Length; i++) _slotRerolls[i] = RerollPerSlot;
    }

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
        var pool = GetAvailablePool(currentPlant);

        // 셔플 후 앞에서 count개
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.GetRange(0, Mathf.Min(count, pool.Count));
    }

    /// <summary>
    /// 후보 한 자리만 다른 아이템으로 교체(카드별 리롤).
    /// 현재 화면에 떠 있는 다른 후보와는 겹치지 않게 고른다. 바꿀 게 없으면 null.
    /// </summary>
    public static SpecialItemData RollReplacement(string currentPlant, List<SpecialItemData> current, int index)
    {
        var pool = GetAvailablePool(currentPlant);

        // 지금 보여주고 있는 후보들 제외 (교체 대상 자기 자신 포함)
        if (current != null)
            pool.RemoveAll(d => current.Contains(d));

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    /// <summary>획득 가능한(미보유 + 조건 충족) 아이템 목록.</summary>
    private static List<SpecialItemData> GetAvailablePool(string currentPlant)
    {
        var pool = new List<SpecialItemData>();
        foreach (var d in All)
        {
            if (d == null || string.IsNullOrEmpty(d.id) || _owned.Contains(d.id)) continue;
            if (d.plantSpecific)
            {
                if (d.plantName != currentPlant) continue;
                if (!d.IsUnlocked()) continue; // 그 식물로 새벽 unlockDawnStage 클리어 필요
            }
            pool.Add(d);
        }
        return pool;
    }

    /// <summary>3택에서 하나 선택 — 보유 등록 + 선물 1개 소모.</summary>
    public static void Acquire(SpecialItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        _owned.Add(item.id);
        PendingGifts = Mathf.Max(0, PendingGifts - 1);
        ResetSlotRerolls(); // 다음 선물을 위해 칸별 리롤 횟수 회복
        Debug.Log($"[SpecialItem] 획득: {item.displayName} ({item.id})");
    }

    // ── 저장/로드 (per-run) ────────────────────────────────────────────────────
    public static List<string> GetSaveOwned() => new List<string>(_owned);
    public static int GetSavePending() => PendingGifts;
    public static List<int> GetSaveRerolls() => new List<int>(_slotRerolls);

    public static void LoadFromSave(List<string> owned, int pending, List<int> slotRerolls = null)
    {
        _owned.Clear();
        if (owned != null) foreach (var id in owned) if (!string.IsNullOrEmpty(id)) _owned.Add(id);
        PendingGifts = Mathf.Max(0, pending);

        ResetSlotRerolls();
        if (slotRerolls != null)
            for (int i = 0; i < _slotRerolls.Length && i < slotRerolls.Count; i++)
                _slotRerolls[i] = Mathf.Clamp(slotRerolls[i], 0, RerollPerSlot);
    }

    /// <summary>새 게임 시작 시 초기화.</summary>
    public static void ResetRun()
    {
        _owned.Clear();
        PendingGifts = 0;
        ResetSlotRerolls();
    }
}
