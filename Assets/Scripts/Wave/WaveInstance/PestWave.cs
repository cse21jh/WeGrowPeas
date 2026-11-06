using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestWave : Wave
{
    public override WaveType WaveType => WaveType.Pest;
    public override string WaveDescription => Loc.T("game.wave.Pest.description");
    public override string WaveSoundString => SoundIds.Pest;
    public const int UnlockStage = 6; // 해충 "웨이브"(날씨) 해금 기준 — 실제 등장 stage 5
    // 벌레 "엔티티"(기어다니는 벌레) 타이밍은 별개 시스템 → BugSchedule / WaveScheduleConfig.Bug 섹션 참조.
}
