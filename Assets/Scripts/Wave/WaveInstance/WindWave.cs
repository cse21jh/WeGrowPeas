using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindWave : Wave
{
    public override WaveType WaveType => WaveType.Wind;
    public override string WaveDescription => "거센 바람이 몰아칩니다......";
    public override string WaveSoundString => "Wind";
    public override int UnlockStage => 5;
}
