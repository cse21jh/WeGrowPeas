using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 이번 판의 특수 아이템. <c>GameRun.Special</c>로 쓴다. 아이템 목록은 <see cref="SpecialItemSystem"/>.
/// - 보유: 이번 판에 획득한 아이템 id 집합. 효과 코드가 <see cref="Has"/>로 조회.
/// - 미수령 선물: 10·20·30일 자유시간에 +1, 수령(3택 선택) 시 -1. 수령 전까지 계속 유지.
/// </summary>
public sealed class SpecialItemState
{
    private readonly HashSet<string> owned = new HashSet<string>();
    private readonly int[] slotRerolls = Enumerable.Repeat(SpecialItemSystem.RerollPerSlot, SpecialItemSystem.SlotCount).ToArray();

    public int PendingGifts { get; private set; }
    public IReadOnlyCollection<string> OwnedIds => owned;

    public bool Has(string id) => !string.IsNullOrEmpty(id) && owned.Contains(id);

    /// <summary>획득한 특수 아이템의 데이터 목록. (정보 앱 표시용)</summary>
    public List<SpecialItemData> GetOwnedItems()
        => owned.Select(SpecialItemSystem.GetData).Where(d => d != null).ToList();

    // ── 리롤 ──────────────────────────────────────────────────────────────────

    /// <summary>해당 칸의 남은 리롤 횟수.</summary>
    public int GetSlotRerolls(int slot)
        => slot >= 0 && slot < slotRerolls.Length ? slotRerolls[slot] : 0;

    public bool CanRerollSlot(int slot) => GetSlotRerolls(slot) > 0;

    /// <summary>해당 칸의 리롤 1회 소모. 성공하면 true.</summary>
    public bool UseSlotReroll(int slot)
    {
        if (!CanRerollSlot(slot)) return false;
        slotRerolls[slot]--;
        return true;
    }

    /// <summary>모든 칸의 리롤 횟수 회복 (새 선물).</summary>
    private void ResetSlotRerolls() => Array.Fill(slotRerolls, SpecialItemSystem.RerollPerSlot);

    // ── 선물 지급/수령 ─────────────────────────────────────────────────────────

    /// <summary>선물 +1 (수령 전까지 유지·누적).</summary>
    public void AddGift() => PendingGifts++;

    /// <summary>선택 후보 3개 롤. 공용 전체 + (현재 식물 + 언락된) 식물별 − 이미 보유.</summary>
    public List<SpecialItemData> RollCandidates(string currentPlant, int count = SpecialItemSystem.SlotCount, Func<SpecialItemData, bool> isUnlocked = null)
    {
        var pool = GetAvailablePool(currentPlant, isUnlocked);

        // 셔플 후 앞에서 count개
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = GameRun.Rng.Item.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.GetRange(0, Math.Min(count, pool.Count));
    }

    /// <summary>
    /// 후보 한 자리만 다른 아이템으로 교체(카드별 리롤).
    /// 현재 화면에 떠 있는 다른 후보와는 겹치지 않게 고른다. 바꿀 게 없으면 null.
    /// </summary>
    public SpecialItemData RollReplacement(string currentPlant, List<SpecialItemData> current)
    {
        var pool = GetAvailablePool(currentPlant);

        // 지금 보여주고 있는 후보들 제외 (교체 대상 자기 자신 포함)
        if (current != null)
            pool.RemoveAll(current.Contains);

        if (pool.Count == 0) return null;
        return pool[GameRun.Rng.Item.Range(0, pool.Count)];
    }

    /// <summary>획득 가능한(미보유 + 조건 충족) 아이템 목록.</summary>
    private List<SpecialItemData> GetAvailablePool(string currentPlant, Func<SpecialItemData, bool> isUnlocked = null)
    {
        var pool = new List<SpecialItemData>();
        foreach (var d in SpecialItemSystem.All)
        {
            if (d == null || string.IsNullOrEmpty(d.id) || owned.Contains(d.id)) continue;
            if (d.plantSpecific)
            {
                if (d.plantName != currentPlant) continue;
                if (!(isUnlocked != null ? isUnlocked(d) : d.IsUnlocked())) continue; // 그 식물로 새벽 unlockDawnStage 클리어 필요
            }
            pool.Add(d);
        }
        return pool;
    }

    /// <summary>3택에서 하나 선택 — 보유 등록 + 선물 1개 소모.</summary>
    public void Acquire(SpecialItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id)) return;
        owned.Add(item.id);
        PendingGifts = Math.Max(0, PendingGifts - 1);
        ResetSlotRerolls(); // 다음 선물을 위해 칸별 리롤 횟수 회복
    }

    // ── 저장/로드 ──────────────────────────────────────────────────────────────

    /// <summary>보유 아이템/미수령 선물/칸별 리롤을 저장 데이터에 담는다. <see cref="LoadFromSave"/>와 짝.</summary>
    public void SaveTo(SpecialItemSave save)
    {
        save.ownedSpecialItems = new List<string>(owned);
        save.pendingSpecialGifts = PendingGifts;
        save.specialItemRerolls = new List<int>(slotRerolls);
    }

    public void LoadFromSave(SpecialItemSave save)
    {
        owned.Clear();
        if (save.ownedSpecialItems != null)
            foreach (var id in save.ownedSpecialItems)
                if (!string.IsNullOrEmpty(id)) owned.Add(id);

        PendingGifts = Math.Max(0, save.pendingSpecialGifts);

        ResetSlotRerolls();
        if (save.specialItemRerolls != null)
            for (int i = 0; i < slotRerolls.Length && i < save.specialItemRerolls.Count; i++)
                slotRerolls[i] = Math.Clamp(save.specialItemRerolls[i], 0, SpecialItemSystem.RerollPerSlot);
    }
}
