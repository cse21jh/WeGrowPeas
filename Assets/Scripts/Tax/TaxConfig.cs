using UnityEngine;

/// <summary>
/// 세금 시스템의 단일 기준 데이터. (WaveScheduleConfig / BugSchedule 와 동일한 패턴)
/// 런타임 접근은 <see cref="TaxSchedule"/>를 통해서 한다.
/// 수치(schedule 값)는 밸런싱 추후 — 지금은 구조용 임시값.
/// </summary>
[CreateAssetMenu(menuName = "Tax/Tax Config", fileName = "TaxConfig")]
public class TaxConfig : ScriptableObject
{
    [Tooltip("세금 납부 주기(일). 기본 5 → 5,10,15… 일차 마감")]
    public int interval = 5;

    [Tooltip("관문별 세금액 고정 표. index 0 = 첫 세금(interval일차). 수치는 밸런싱 TBD")]
    public int[] schedule = { 1000, 3000, 8000, 22000, 60000, 150000 };

    [Tooltip("표를 넘어선 관문의 세금 = 마지막 표값 × growth^(초과 횟수). 지수적으로 계속 상승")]
    public float beyondTableGrowth = 2f;
}
