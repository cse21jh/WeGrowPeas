using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestWave : Wave
{
    public override WaveType WaveType => WaveType.Pest;
    public override string WaveName => "해충";
    public override string WaveDescription => "불길한 날개짓이\n들립니다......";
    public override string WaveSoundString => "Pest";
    public const int UnlockStage = 6; // 해충 "웨이브"(날씨) 해금 기준 — 실제 등장 stage 5
    // 벌레 "엔티티"(기어다니는 벌레) 타이밍은 별개 시스템 → BugSchedule / WaveScheduleConfig.Bug 섹션 참조.
}
