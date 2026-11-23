using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 치장 아이템 목록. 처음 쓸 때 Resources에서 한 번 읽고 종류별로 나눠 둔다.
/// </summary>
public static class CustomizeCatalog
{
    private static Dictionary<CustomizeCategory, List<CustomizeItemData>> _byCategory;
    private static Dictionary<string, CustomizeItemData> _byId;

    // 이 프로젝트는 Play 진입 시 도메인 리로드를 끄고 쓴다(Enter Play Mode Options).
    // 그러면 static 값이 이전 Play나 에디터에서 읽은 그대로 남아, 에셋을 추가·수정해도 반영되지 않는다.
    // Play가 시작될 때마다 비워서 처음 쓸 때 다시 읽게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _byCategory = null;
        _byId = null;
    }

    private static void EnsureLoaded()
    {
        if (_byCategory != null) return;

        _byCategory = new Dictionary<CustomizeCategory, List<CustomizeItemData>>();
        _byId = new Dictionary<string, CustomizeItemData>();
        foreach (CustomizeCategory c in Enum.GetValues(typeof(CustomizeCategory)))
            _byCategory[c] = new List<CustomizeItemData>();

        foreach (var item in Resources.LoadAll<CustomizeItemData>(ResourcePaths.CustomizeItems))
        {
            if (!_byId.TryAdd(item.Id, item))
            {
                Debug.LogWarning($"[Customize] 같은 이름의 치장 아이템이 둘 있습니다: {item.Id}");
                continue;
            }
            _byCategory[item.category].Add(item);
        }

        var order = Resources.Load<CustomizeItemOrder>(ResourcePaths.CustomizeItemOrder);
        if (order == null) Debug.LogWarning($"[Customize] 순서 에셋({ResourcePaths.CustomizeItemOrder})이 없어 에셋 이름 순으로 보입니다.");
        foreach (var pair in _byCategory)
            SortByOrder(pair.Value, order != null ? order.Get(pair.Key) : null);
    }

    /// <summary>
    /// 순서 에셋에 적힌 순서대로 정렬한다. 목록에 없는 아이템은 맨 뒤에 에셋 이름 순.
    /// 같은 아이템이 두 번 적혀 있으면 앞의 것을 쓴다.
    /// </summary>
    public static void SortByOrder(List<CustomizeItemData> items, IReadOnlyList<CustomizeItemData> order)
    {
        var rank = new Dictionary<CustomizeItemData, int>();
        if (order != null)
            for (int i = 0; i < order.Count; i++)
                if (order[i] != null && !rank.ContainsKey(order[i])) rank[order[i]] = i;

        items.Sort((a, b) =>
        {
            bool hasA = rank.TryGetValue(a, out int ra);
            bool hasB = rank.TryGetValue(b, out int rb);
            if (hasA && hasB) return ra.CompareTo(rb);
            if (hasA != hasB) return hasA ? -1 : 1;
            return string.CompareOrdinal(a.name, b.name);
        });
    }

    /// <summary>한 종류의 아이템 전부(잠긴 것 포함), 표시 순서대로.</summary>
    public static IReadOnlyList<CustomizeItemData> Get(CustomizeCategory category)
    {
        EnsureLoaded();
        return _byCategory[category];
    }

    /// <summary>id(에셋 이름)로 찾는다. 없으면 null.</summary>
    public static CustomizeItemData Find(string id)
    {
        EnsureLoaded();
        return id != null && _byId.TryGetValue(id, out var item) ? item : null;
    }

    /// <summary>그 종류의 기본 아이템. 지정된 게 없으면 첫 아이템, 아이템이 없으면 null.</summary>
    public static CustomizeItemData GetDefault(CustomizeCategory category)
    {
        var list = Get(category);
        foreach (var item in list)
            if (item.isDefault) return item;
        return list.Count > 0 ? list[0] : null;
    }
}
