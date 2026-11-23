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
///
/// 기본 장식품(<see cref="CustomizeItemData.placedByDefault"/>, 원래 배경에 있던 통나무·돌 등)은 프로필을 읽을 때
/// 아직 놓아 준 적이 없으면 정해진 자리에 놓아 주고, 놓아 준 id를 프로필에 기록한다.
/// 기록된 장식품은 플레이어가 치워도 다시 놓지 않는다.
/// </summary>
public static class DecorationLayout
{
    private static readonly List<DecorationPlacement> _current = new List<DecorationPlacement>();
    private static readonly List<DecorationPlacement> _saved = new List<DecorationPlacement>();
    private static readonly HashSet<string> _seededDefaults = new HashSet<string>();

    /// <summary>지금 배치가 바뀌었다(놓기, 치우기, 되돌리기, 프로필 읽기).</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _current.Clear();
        _saved.Clear();
        _seededDefaults.Clear();
        Changed = null;
    }

    /// <summary>지금 배치. 보유하지 않은 장식품(다른 프로필 등)도 들어 있을 수 있으니 쓰는 쪽에서 거른다.</summary>
    public static IReadOnlyList<DecorationPlacement> Current => _current;

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    public static void SaveTo(CustomizeProfile profile)
    {
        profile.decorations = new List<DecorationPlacement>();
        foreach (var p in _saved) profile.decorations.Add(p.Clone());
        profile.seededDecorations = new List<string>(_seededDefaults);
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

        _seededDefaults.Clear();
        if (profile?.seededDecorations != null)
            foreach (var id in profile.seededDecorations)
                if (!string.IsNullOrEmpty(id)) _seededDefaults.Add(id);
        SeedDefaults();

        CopyInto(_saved, _current);
        Changed?.Invoke();
    }

    /// <summary>
    /// 아직 놓아 준 적 없는 기본 장식품을 저장된 배치에 넣는다. 이미 놓여 있으면 넣지 않는다(같은 장식품은 하나만).
    /// 파일 저장은 따로 요청하지 않는다. 저장 전에 게임을 꺼도 다음에 읽을 때 같은 결과로 다시 놓인다.
    /// </summary>
    private static void SeedDefaults()
    {
        foreach (var item in CustomizeCatalog.Get(CustomizeCategory.Decoration))
        {
            if (!item.placedByDefault || !_seededDefaults.Add(item.Id)) continue;
            if (_saved.Exists(p => p.itemId == item.Id)) continue;
            _saved.Add(new DecorationPlacement { itemId = item.Id, x = item.defaultPosition.x, y = item.defaultPosition.y });
        }
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
