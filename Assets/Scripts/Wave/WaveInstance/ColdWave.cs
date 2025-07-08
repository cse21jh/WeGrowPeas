using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdWave : Wave
{
    public override WaveType WaveType => WaveType.Cold;
    public override string WaveDescription => "거센 바람이 몰아칩니다......";
    public override string WaveSoundString => "Cold";
    public override int UnlockStage => 20;
}
