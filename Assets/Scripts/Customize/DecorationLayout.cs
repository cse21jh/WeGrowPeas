using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 놓인 장식품의 위치·크기·기울기·좌우 반전. 크기는 아이템 기본 크기(<see cref="CustomizeItemData.placedHeight"/>)에 곱하는 배율이고,
/// 기울기·반전은 아이콘 그림 그대로(0도, 뒤집지 않음)를 기준으로 한 값이다.
/// 처음부터 놓여 있는 배경 소품은 기울기·반전을 담은 모습(<see cref="DefaultFor"/>)으로 놓인다.
/// </summary>
public struct DecorationPose
{
    /// <summary>
    /// 플레이어가 조절할 수 있는 크기 배율의 바깥 한계. 실제 범위는 장식품마다 보이는 크기에 맞춰 이 안에서 더 좁힌다
    /// (<see cref="DecorationLayer.ScaleRange"/>).
    /// </summary>
    public const float MinScale = 0.5f;
    public const float MaxScale = 3f;

    /// <summary>월드 위치(스프라이트의 피벗 위치).</summary>
    public Vector2 position;
    /// <summary>크기 배율. 1이 아이템 기본 크기.</summary>
    public float scale;
    /// <summary>더 기울인 각도(도, +는 반시계). 0이 아이템 기본 기울기.</summary>
    public float rotation;
    /// <summary>좌우 반전.</summary>
    public bool flipX;

    public DecorationPose(Vector2 position, float scale = 1f, float rotation = 0f, bool flipX = false)
    {
        this.position = position;
        this.scale = scale;
        this.rotation = rotation;
        this.flipX = flipX;
    }

    /// <summary>
    /// 처음부터 놓여 있는 장식품의 모습. 배경에 박혀 있던 자리·기울기·반전 그대로다
    /// (<see cref="CustomizeItemData.defaultPosition"/>, <see cref="CustomizeItemData.placedRotation"/>, <see cref="CustomizeItemData.placedFlipX"/>).
    /// </summary>
    public static DecorationPose DefaultFor(CustomizeItemData item)
        => new DecorationPose(item.defaultPosition, 1f, item.placedRotation, item.placedFlipX);

    public bool SameAs(DecorationPose o)
        => Mathf.Approximately(position.x, o.position.x) && Mathf.Approximately(position.y, o.position.y)
           && Mathf.Approximately(scale, o.scale) && Mathf.Approximately(rotation, o.rotation) && flipX == o.flipX;
}

/// <summary>농장에 놓인 장식품 하나. 저장 파일에 그대로 들어간다.</summary>
[Serializable]
public class DecorationPlacement
{
    /// <summary>장식품 아이템 id(에셋 이름).</summary>
    public string itemId;

    /// <summary>놓인 월드 위치(스프라이트의 피벗 위치).</summary>
    public float x;
    public float y;

    /// <summary>크기 배율(1 = 기본). 이 칸이 없던 예전 저장 파일은 0으로 읽힐 수 있어 0 이하는 1로 본다(<see cref="Pose"/>).</summary>
    public float scale = 1f;

    /// <summary>더 기울인 각도(도).</summary>
    public float rotation;

    /// <summary>좌우 반전.</summary>
    public bool flipX;

    public Vector2 Position => new Vector2(x, y);

    public DecorationPose Pose => new DecorationPose(Position, scale > 0f ? scale : 1f, rotation, flipX);

    public void SetPose(DecorationPose pose)
    {
        x = pose.position.x;
        y = pose.position.y;
        scale = Mathf.Clamp(pose.scale, DecorationPose.MinScale, DecorationPose.MaxScale);
        rotation = Mathf.Repeat(pose.rotation + 180f, 360f) - 180f;
        flipX = pose.flipX;
    }

    public DecorationPlacement Clone() => new DecorationPlacement
    {
        itemId = itemId, x = x, y = y, scale = scale, rotation = rotation, flipX = flipX,
    };
}

/// <summary>
/// 농장에 놓인 장식품 목록. <see cref="CustomizeState"/>처럼 두 벌을 들고 있다.
///   지금 배치   → 치장 화면에서 놓고·치우고·고칠 때마다 바로 바뀐다(<see cref="Place"/> / <see cref="Remove"/> / <see cref="SetPose"/>).
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
        UpgradeOldDefaultPlacements();
        SeedDefaults();

        CopyInto(_saved, _current);
        Changed?.Invoke();
    }

    /// <summary>
    /// 아직 놓아 준 적 없는 기본 장식품을 저장된 배치에 넣는다. 배경에 박혀 있던 자리·기울기·반전 그대로 놓는다.
    /// 이미 놓여 있으면 넣지 않는다(같은 장식품은 하나만).
    /// 파일 저장은 따로 요청하지 않는다. 저장 전에 게임을 꺼도 다음에 읽을 때 같은 결과로 다시 놓인다.
    /// </summary>
    private static void SeedDefaults()
    {
        foreach (var item in CustomizeCatalog.Get(CustomizeCategory.Decoration))
        {
            if (!item.placedByDefault || !_seededDefaults.Add(item.Id)) continue;
            if (_saved.Exists(p => p.itemId == item.Id)) continue;
            var placement = new DecorationPlacement { itemId = item.Id };
            placement.SetPose(DecorationPose.DefaultFor(item));
            _saved.Add(placement);
        }
    }

    /// <summary>
    /// 예전 저장 파일 맞추기. 예전에는 아이템의 기본 기울기·반전(통나무 기울기, 작은 돌 반전)을 놓을 때마다 따로 더했기 때문에,
    /// 처음 놓아 준 그대로인 배치는 기울기 0·반전 없음으로 저장돼 있다. 지금은 그 값을 배치 안에 담으므로 같은 모습이 되게 옮겨 준다.
    /// 처음 자리에 크기·기울기·반전을 하나도 안 바꾼 배치만 고친다(지금 방식으로 놓인 배치는 이미 기울기·반전이 들어 있어 해당하지 않는다).
    /// </summary>
    private static void UpgradeOldDefaultPlacements()
    {
        foreach (var p in _saved)
        {
            var item = CustomizeCatalog.Find(p.itemId);
            if (item == null || !item.placedByDefault) continue;
            if (Mathf.Approximately(item.placedRotation, 0f) && !item.placedFlipX) continue;

            var pose = p.Pose;
            bool untouchedOld = Mathf.Approximately(pose.scale, 1f) && Mathf.Approximately(pose.rotation, 0f) && !pose.flipX
                                && (pose.position - item.defaultPosition).sqrMagnitude < 0.0001f;
            if (untouchedOld) p.SetPose(DecorationPose.DefaultFor(item));
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

    /// <summary>놓여 있는 장식품의 위치·크기·기울기·반전을 바꾼다(옮기기, 크기 조절, 회전, 좌우 반전).</summary>
    public static void SetPose(DecorationPlacement placement, DecorationPose pose)
    {
        if (placement == null || !_current.Contains(placement) || placement.Pose.SameAs(pose)) return;
        placement.SetPose(pose);
        Changed?.Invoke();
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
                if (a.itemId != b.itemId || !a.Pose.SameAs(b.Pose))
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
