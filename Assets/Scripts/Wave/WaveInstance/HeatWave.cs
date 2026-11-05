using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatWave : Wave
{
    public override WaveType WaveType => WaveType.Heat;
    public override string WaveDescription => Loc.T("game.wave.Heat.description");
    public override string WaveSoundString => "Heat";
    public const int UnlockStage = 26;
}
