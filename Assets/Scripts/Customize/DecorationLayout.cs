using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>농장에 놓인 장식품 하나. 저장 파일에 그대로 들어간다.</summary>
[Serializable]
public class DecorationPlacement
{
    /// <summary>장식품 아이템 id(에셋 이름).</summary>
    public string itemId;

    /// <summary>놓인 월드 위치(스프라이트의 피벗 위치).</summary>
    public float x;
    public float y;

    public Vector2 Position => new Vector2(x, y);

    public DecorationPlacement Clone() => new DecorationPlacement { itemId = itemId, x = x, y = y };
}

/// <summary>
/// 농장에 놓인 장식품 목록. <see cref="CustomizeState"/>처럼 두 벌을 들고 있다.
///   지금 배치   → 치장 화면에서 놓고 치울 때마다 바로 바뀐다(<see cref="Add"/> / <see cref="Remove"/>).
///   저장된 배치 → <see cref="Save"/>를 눌렀을 때만 지금 배치로 맞춰지고, 프로필 파일에는 이쪽만 쓴다.
///
/// 저장·되돌리기·프로필 읽기는 <see cref="CustomizeState"/>가 함께 불러 준다(치장 화면의 저장 버튼 하나로 같이 저장).
/// 씬에 보이는 장식품은 <see cref="DecorationLayer"/>가 이 목록을 읽어 만든다.
/// 같은 장식품은 하나만 놓을 수 있다.
/// </summary>
public static class DecorationLayout
{
    private static readonly List<DecorationPlacement> _current = new List<DecorationPlacement>();
    private static readonly List<DecorationPlacement> _saved = new List<DecorationPlacement>();

    /// <summary>지금 배치가 바뀌었다(놓기, 치우기, 되돌리기, 프로필 읽기).</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _current.Clear();
        _saved.Clear();
        Changed = null;
    }

    /// <summary>지금 배치. 보유하지 않은 장식품(다른 프로필 등)도 들어 있을 수 있으니 쓰는 쪽에서 거른다.</summary>
    public static IReadOnlyList<DecorationPlacement> Current => _current;

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    public static void SaveTo(CustomizeProfile profile)
    {
        profile.decorations = new List<DecorationPlacement>();
        foreach (var p in _saved) profile.decorations.Add(p.Clone());
    }

    public static void LoadFrom(CustomizeProfile profile)
    {
        _saved.Clear();
        if (profile?.decorations != null)
        {
            var seen = new HashSet<string>();
            foreach (var p in profile.decorations)
                if (p != null && !string.IsNullOrEmpty(p.itemId) && seen.Add(p.itemId)) _saved.Add(p.Clone());
        }

        CopyInto(_saved, _current);
        Changed?.Invoke();
    }

    // ── 놓기 / 치우기 ─────────────────────────────────────────────────────────

    /// <summary>장식품을 그 자리에 놓는다. 이미 놓여 있는 장식품이면 놓지 않고 null.</summary>
    public static DecorationPlacement Place(CustomizeItemData item, Vector2 position)
    {
        if (item == null || item.category != CustomizeCategory.Decoration || IsPlaced(item)) return null;
        var p = new DecorationPlacement { itemId = item.Id, x = position.x, y = position.y };
        _current.Add(p);
        Changed?.Invoke();
        return p;
    }

    public static void Remove(DecorationPlacement placement)
    {
        if (_current.Remove(placement)) Changed?.Invoke();
    }

    /// <summary>그 장식품이 지금 놓여 있는가.</summary>
    public static bool IsPlaced(CustomizeItemData item) => Find(item) != null;

    private static DecorationPlacement Find(CustomizeItemData item)
    {
        if (item == null) return null;
        foreach (var p in _current)
            if (p.itemId == item.Id) return p;
        return null;
    }

    // ── 저장 / 되돌리기 ──────────────────────────────────────────────────────

    public static bool HasUnsavedChanges
    {
        get
        {
            if (_current.Count != _saved.Count) return true;
            for (int i = 0; i < _current.Count; i++)
            {
                var a = _current[i];
                var b = _saved[i];
                if (a.itemId != b.itemId || !Mathf.Approximately(a.x, b.x) || !Mathf.Approximately(a.y, b.y))
                    return true;
            }
            return false;
        }
    }

    /// <summary>지금 배치를 저장된 배치로 맞춘다. 파일 저장 요청은 <see cref="CustomizeState.Save"/>가 한다.</summary>
    public static void Save() => CopyInto(_current, _saved);

    public static void RevertToSaved()
    {
        if (!HasUnsavedChanges) return;
        CopyInto(_saved, _current);
        Changed?.Invoke();
    }

    private static void CopyInto(List<DecorationPlacement> from, List<DecorationPlacement> to)
    {
        to.Clear();
        foreach (var p in from) to.Add(p.Clone());
    }
}
