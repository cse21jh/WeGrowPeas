using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdWave : Wave
{
    public override WaveType WaveType => WaveType.Cold;
    public override string WaveName => "추위";
    public override string WaveDescription => "기온이 떨어지고\n있습니다.....";
    public override string WaveSoundString => "Cold";
    public const int UnlockStage = 26;
}
