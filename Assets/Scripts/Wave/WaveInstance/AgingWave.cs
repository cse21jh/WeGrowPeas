using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgingWave : Wave
{
    public override WaveType WaveType => WaveType.Aging;
    public override string WaveDescription => "곧 하루가\n지나갑니다......";
    public override string WaveSoundString => "Aging";
    public const int UnlockStage = 1;
}
