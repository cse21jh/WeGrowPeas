using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoneWave : Wave
{
    public override WaveType WaveType => WaveType.None;
    public override string WaveDescription => "오늘은 아무 일도 일어나지 않을 것 같습니다..";
    public override string WaveSoundString => "Aging";
    public override int UnlockStage => 999;
}
