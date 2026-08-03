using System.Collections.Generic;
using UnityEngine;

public enum CurseType { Temporal, Seasonal }

/// <summary>
/// 저주 1종의 정적 데이터. 실제 수치는 <see cref="levels"/>에 1/2/3단계별로 담고,
/// 런타임에는 새벽 저주 레벨(<c>DawnSystem.Current.curseLevel</c>)로 해당 레벨을 골라 쓴다.
/// 필드 의미는 저주마다 다르므로 각 Curse 인스턴스 클래스 주석 참고.
/// </summary>
[CreateAssetMenu(menuName = "Curse/CurseItem")]
[BalanceGroup("Curse", "Curses")]
public class CurseScriptable : ScriptableObject
{
    public string curseId;

    public CurseType curseType;

    public string title;

    public Sprite icon;

    [TextArea] public string description;

    [Balance("해금 스테이지")]
    [Tooltip("해금 스테이지 (이 스테이지 이상부터 등장)")]
    public int unlockStage = 1;

    [BalanceRows("level")] // 저주 1/2/3단계를 한 행씩 표에 펼침 (키 = 순번)
    [Tooltip("1/2/3단계 수치. index 0 = 1단계. 새벽 저주 레벨로 선택됨.")]
    public List<CurseLevel> levels = new List<CurseLevel>();

    /// <summary>주어진 레벨(1~3)의 수치. 범위를 벗어나면 가장 가까운 레벨로 클램프.</summary>
    public CurseLevel GetLevel(int level)
    {
        if (levels == null || levels.Count == 0) return null;
        int idx = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        return levels[idx];
    }
}

/// <summary>
/// 저주 단계별 수치(제네릭). 저주마다 필요한 값 개수가 달라 공용 필드로 둔다.
/// - <see cref="valueA"/>: 주 수치 (대부분의 저주가 사용)
/// - <see cref="valueB"/>: 보조 수치 (예: 독점시장 상한, 범위형 저주)
/// - <see cref="days"/>: 지속/영향 일수 (예: 기상이변, 이중 웨이브)
/// 각 필드의 실제 의미는 해당 Curse 인스턴스 클래스에 문서화.
/// </summary>
[System.Serializable]
public class CurseLevel
{
    [Balance("수치A")] public float valueA;
    [Balance("수치B")] public float valueB;
    [Balance("일수")] public int days;

    [TextArea]
    [Tooltip("인스펙터 메모용. 로직에 영향 없음.")]
    public string note;
}
