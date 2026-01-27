using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatWave : Wave
{
    public override WaveType WaveType => WaveType.Heat;
    public override string WaveName => "더위";
    public override string WaveDescription => "기온이 올라가고\n있습니다......";
    public override string WaveSoundString => "Heat";
    public const int UnlockStage = 26;
}
