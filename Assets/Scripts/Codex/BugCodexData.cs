using UnityEngine;

/// <summary>
/// 도감 "벌레" 항목 데이터. Bug 클래스에는 설명/아이콘이 없어 별도로 둔다.
/// <see cref="bugId"/>는 Bug 클래스 이름(GetType().Name)과 일치시켜 발견/카운트와 연결.
/// </summary>
[CreateAssetMenu(menuName = "Codex/Bug Codex Data")]
[LocGroup(LocPrefix, "name", "description")]
public class BugCodexData : ScriptableObject
{
    private const string LocPrefix = "bug";

    /// <summary>화면에 보여줄 이름.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, name, "name"));

    /// <summary>화면에 보여줄 설명.</summary>
    public string LocalizedDescription => Loc.Or(LocKey.For(LocPrefix, name, "description"), "");

    [Tooltip("Bug 클래스 이름과 일치 (예: DefaultBug, StraightMovingBug, Ladybug)")]
    public string bugId;

    public Sprite icon;
}
