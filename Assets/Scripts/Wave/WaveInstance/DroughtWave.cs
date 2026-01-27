using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroughtWave : Wave
{
    public override WaveType WaveType => WaveType.Drought;
    public override string WaveName => "가뭄";
    public override string WaveDescription => "땅이 메마르기\n시작합니다......";
    public override string WaveSoundString => "Drought";
    public const int UnlockStage = 21;
}
