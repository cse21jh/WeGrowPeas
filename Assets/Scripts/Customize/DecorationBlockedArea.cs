using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장식품을 놓을 수 없는 구역(텃밭). 치장 씬에 둔다.
/// 영역은 붙어 있는 SpriteRenderer(1×1 사각형 그림)의 크기·위치 그대로이고,
/// 그 SpriteRenderer는 장식품을 배치하는 동안에만 켜져서 구역을 어둡게 덮는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DecorationBlockedArea : MonoBehaviour
{
    private static readonly List<DecorationBlockedArea> _all = new List<DecorationBlockedArea>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _all.Clear();

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.enabled = false;
    }

    private void OnEnable() => _all.Add(this);
    private void OnDisable() => _all.Remove(this);

    /// <summary>막힌 구역(2D). 렌더러를 끈 상태에서도 그림 크기와 Transform으로 계산한다.</summary>
    public Rect Area
    {
        get
        {
            var b = _sr.sprite != null ? _sr.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            Vector2 size = Vector2.Scale(b.size, transform.lossyScale);
            Vector2 center = transform.TransformPoint(b.center);
            return new Rect(center - size * 0.5f, size);
        }
    }

    /// <summary>그 영역이 어느 막힌 구역과 조금이라도 겹치는가.</summary>
    public static bool OverlapsAny(Rect rect)
    {
        foreach (var area in _all)
            if (area.Area.Overlaps(rect)) return true;
        return false;
    }

    /// <summary>모든 막힌 구역을 어둡게 표시하거나 숨긴다.</summary>
    public static void SetAllDimmed(bool on)
    {
        foreach (var area in _all) area._sr.enabled = on;
    }
}
