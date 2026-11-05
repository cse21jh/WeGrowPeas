using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindWave : Wave
{
    public override WaveType WaveType => WaveType.Wind;
    public override string WaveDescription => Loc.T("game.wave.Wind.description");
    public override string WaveSoundString => "Wind";
    public const int UnlockStage = 11;
}
