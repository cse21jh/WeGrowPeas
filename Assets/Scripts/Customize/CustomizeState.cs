using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 장착 중인 치장 아이템(종류마다 하나).
/// 아무것도 고르지 않은 종류는 그 종류의 기본 아이템(isDefault)이 장착된 것으로 본다.
/// 보유(<see cref="CustomizeInventory"/>)한 아이템만 장착할 수 있다.
///
/// 장착 상태는 두 벌을 들고 있다.
///   지금 상태   → 치장 화면에서 고를 때마다 바로 바뀐다(<see cref="Equip"/>).
///   저장된 상태 → <see cref="Save"/>를 눌렀을 때만 지금 상태로 맞춰지고, 프로필 파일에는 이쪽만 쓴다.
/// <see cref="RevertToSaved"/>는 지금 상태를 저장된 상태로 되돌리고,
/// <see cref="HasUnsavedChanges"/>로 저장하지 않은 변경이 있는지 알 수 있다.
///
/// 저장 흐름은 다른 프로필 시스템과 같다(SaveManager가 LoadFrom / SaveTo를 부른다).
/// </summary>
public static class CustomizeState
{
    private static readonly Dictionary<CustomizeCategory, CustomizeItemData> _equipped
        = new Dictionary<CustomizeCategory, CustomizeItemData>();

    private static readonly Dictionary<CustomizeCategory, CustomizeItemData> _saved
        = new Dictionary<CustomizeCategory, CustomizeItemData>();

    /// <summary>장착이 바뀌었다(새로 장착한 아이템).</summary>
    public static event Action<CustomizeItemData> EquippedChanged;

    /// <summary>프로필에서 장착 상태를 새로 읽었다(게임 시작, 프로필 전환 등). 모든 종류가 바뀌었을 수 있다.</summary>
    public static event Action Loaded;

    // 도메인 리로드를 끈 상태에서도 Play마다 처음 상태로 시작하게 한다. (CustomizeCatalog 참고)
    // 실제 값은 그 뒤 SaveManager가 프로필을 읽으면서 채운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _equipped.Clear();
        _saved.Clear();
        EquippedChanged = null;
        Loaded = null;
    }

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    /// <summary>프로필에 담는다. 지금 상태가 아니라 마지막으로 저장한 상태를 쓴다. <see cref="LoadFrom"/>과 짝.</summary>
    public static void SaveTo(CustomizeProfile profile)
    {
        profile.equippedIds = new List<string>();
        foreach (var item in _saved.Values)
            if (item != null) profile.equippedIds.Add(item.Id);
    }

    /// <summary>프로필에서 읽는다. null이면 새 프로필(전부 기본 아이템). 지금 상태도 저장된 상태로 맞춘다.</summary>
    public static void LoadFrom(CustomizeProfile profile)
    {
        _saved.Clear();
        if (profile?.equippedIds != null)
        {
            foreach (var id in profile.equippedIds)
            {
                var item = CustomizeCatalog.Find(id);
                if (item != null) _saved[item.category] = item;
            }
        }

        _equipped.Clear();
        foreach (var pair in _saved) _equipped[pair.Key] = pair.Value;

        Loaded?.Invoke();
    }

    // ── 장착 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 그 종류에 지금 장착된 아이템. 고른 적이 없거나, 고른 아이템을 지금 프로필이 보유하지 않으면
    /// (예: 다른 프로필로 바꾼 뒤) 기본 아이템. 아이템이 하나도 없으면 null.
    /// </summary>
    public static CustomizeItemData GetEquipped(CustomizeCategory category) => Resolve(_equipped, category);

    /// <summary>그 종류에 마지막으로 저장된 아이템. 규칙은 <see cref="GetEquipped"/>와 같다.</summary>
    public static CustomizeItemData GetSaved(CustomizeCategory category) => Resolve(_saved, category);

    public static bool IsEquipped(CustomizeItemData item)
        => item != null && GetEquipped(item.category) == item;

    /// <summary>아이템을 장착한다. 같은 종류의 이전 아이템은 자동으로 빠진다. 보유하지 않은 아이템은 장착할 수 없다.</summary>
    public static void Equip(CustomizeItemData item)
    {
        if (item == null || IsEquipped(item)) return;
        if (!CustomizeInventory.IsOwned(item)) return;
        _equipped[item.category] = item;
        EquippedChanged?.Invoke(item);
    }

    // ── 저장 / 되돌리기 ──────────────────────────────────────────────────────

    /// <summary>지금 장착 상태가 마지막으로 저장한 상태와 다른가.</summary>
    public static bool HasUnsavedChanges
    {
        get
        {
            foreach (CustomizeCategory c in Enum.GetValues(typeof(CustomizeCategory)))
                if (GetEquipped(c) != GetSaved(c)) return true;
            return false;
        }
    }

    /// <summary>지금 장착 상태를 저장한다(프로필 파일에도 기록).</summary>
    public static void Save()
    {
        _saved.Clear();
        foreach (CustomizeCategory c in Enum.GetValues(typeof(CustomizeCategory)))
        {
            var item = GetEquipped(c);
            if (item != null) _saved[c] = item;
        }
        SaveManager.RequestProfileSave();
        Debug.Log("[Customize] 치장 상태 저장");
    }

    /// <summary>지금 장착 상태를 마지막으로 저장한 상태로 되돌린다. 바뀐 종류마다 <see cref="EquippedChanged"/>를 보낸다.</summary>
    public static void RevertToSaved()
    {
        foreach (CustomizeCategory c in Enum.GetValues(typeof(CustomizeCategory)))
        {
            var saved = GetSaved(c);
            if (GetEquipped(c) == saved) continue;

            if (saved != null) _equipped[c] = saved;
            else _equipped.Remove(c);
            if (saved != null) EquippedChanged?.Invoke(saved);
        }
    }

    private static CustomizeItemData Resolve(Dictionary<CustomizeCategory, CustomizeItemData> map, CustomizeCategory category)
    {
        if (map.TryGetValue(category, out var item) && CustomizeInventory.IsOwned(item)) return item;
        return CustomizeCatalog.GetDefault(category);
    }
}
