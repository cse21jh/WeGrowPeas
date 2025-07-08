using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PestWave : Wave
{
    public override WaveType WaveType => WaveType.Pest;
    public override string WaveDescription => "불길한 날개소리가 들립니다......";
    public override string WaveSoundString => "Pest";
    public override int UnlockStage => 15;
}
