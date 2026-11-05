using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdWave : Wave
{
    public override WaveType WaveType => WaveType.Cold;
    public override string WaveDescription => Loc.T("game.wave.Cold.description");
    public override string WaveSoundString => "Cold";
    public const int UnlockStage = 26;
}
