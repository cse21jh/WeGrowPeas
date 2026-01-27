using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainWave : Wave
{
    public override WaveType WaveType => WaveType.HeavyRain;
    public override string WaveName => "폭우";
    public override string WaveDescription => "폭우가 내리기\n시작합니다......";
    public override string WaveSoundString => "HeavyRain";
    public const int UnlockStage = 21;
}
