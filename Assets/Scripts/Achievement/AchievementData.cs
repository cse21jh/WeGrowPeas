using System.Text.RegularExpressions;
using UnityEngine;

public enum AchievementCondition
{
    /// <summary>누적 통계(<see cref="CodexProgress"/>)가 목표치 이상이면 달성. 에셋만 추가하면 된다.</summary>
    LifetimeStat,

    /// <summary>코드에서 <see cref="AchievementSystem.Unlock"/>을 직접 불러 달성. 조건이 복잡한 업적용.</summary>
    Manual,
}

/// <summary>
/// 업적 정의 1개. Resources/Data/Achievements 아래에 두면 자동으로 읽힌다.
/// </summary>
[CreateAssetMenu(menuName = "Achievement/Achievement Data", fileName = "NEW_ACHIEVEMENT")]
public class AchievementData : ScriptableObject
{
    [Tooltip("영구 키이자 Steam 업적 API 이름. 영문 대문자·숫자·_ 만. 바꾸면 기존 달성 기록과 연결이 끊긴다.")]
    public string id;

    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("달성 전에는 도감에 ??? 로 표시")]
    public bool hidden;

    [Tooltip("도감 목록 정렬 순서(작을수록 위)")]
    public int sortOrder;

    [Header("조건")]
    public AchievementCondition condition = AchievementCondition.LifetimeStat;

    [Tooltip("LifetimeStat: CodexProgress 통계 키 (예: sold_pea, sold_peanut, bugkill_total)")]
    public string statKey;

    [Min(1)] public int target = 1;

    private static readonly Regex IdPattern = new Regex("^[A-Z0-9_]+$");

    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(id) && !IdPattern.IsMatch(id))
            Debug.LogWarning($"[Achievement] id는 Steam API 이름으로도 쓰이므로 영문 대문자·숫자·_ 만 써야 합니다: {id}", this);
        if (condition == AchievementCondition.LifetimeStat && string.IsNullOrEmpty(statKey))
            Debug.LogWarning($"[Achievement] {id}: LifetimeStat인데 statKey가 비어 있습니다.", this);
    }
}
