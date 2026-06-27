using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 웨이브의 "타이밍 단일 기준". 한 웨이브의 해금/계절/등장/경고 메시지 타이밍을
/// 이 한 곳에서만 정의하고, 나머지 시스템(EnemyController·상점·메신저)은 전부 여기서 파생한다.
/// 런타임 접근은 <see cref="WaveSchedule"/>를 통해서 한다.
/// 에셋은 Tools/Create Wave Schedule Config 메뉴로 생성한다 (Assets/Resources/Data/WaveScheduleConfig.asset).
/// </summary>
[CreateAssetMenu(menuName = "Wave/Wave Schedule Config", fileName = "WaveScheduleConfig")]
public class WaveScheduleConfig : ScriptableObject
{
    [Tooltip("계절 하나의 길이(스테이지 수). 기본 5")]
    public int seasonLength = 5;

    [Tooltip("상점 형질/웨이브 해금을 실제 첫 등장보다 며칠 일찍 열지. 기본 2 (\"곧 등장해요\" 사전 경고와 동기화)")]
    public int shopUnlockLeadStages = 2;

    public List<WaveScheduleEntry> waves = new List<WaveScheduleEntry>();
}

[System.Serializable]
public class WaveScheduleEntry
{
    public WaveType waveType;

    [Tooltip("이 웨이브의 가중치가 켜지는 기준 스테이지(eligibility). 실제 첫 등장은 계절·미리보기 반영 후 파생됨")]
    public int unlockStage = 999;

    [Tooltip("등장 가능한 계절(비우면 모든 계절)")]
    public Season[] allowedSeasons;

    [Tooltip("첫 등장 밤에 발송할 메신저 트리거 id(예: PestUnlock). 비우면 메시지 없음")]
    public string unlockTriggerId;
}
