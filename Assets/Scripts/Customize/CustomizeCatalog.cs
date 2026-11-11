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

        foreach (var list in _byCategory.Values)
            list.Sort((a, b) => a.sortOrder != b.sortOrder
                ? a.sortOrder.CompareTo(b.sortOrder)
                : string.CompareOrdinal(a.name, b.name));
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
