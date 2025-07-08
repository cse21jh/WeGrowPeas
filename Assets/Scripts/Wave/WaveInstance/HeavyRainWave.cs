using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainWave : Wave
{
    public override WaveType WaveType => WaveType.HeavyRain;
    public override string WaveDescription => "폭우가 내리기 시작합니다......";
    public override string WaveSoundString => "HeavyRain";
    public override int UnlockStage => 25;
}
