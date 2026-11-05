using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoneWave : Wave
{
    public override WaveType WaveType => WaveType.None;
    public override string WaveName => "";
    public override string WaveDescription => Loc.T("game.wave.None.description");
    public override string WaveSoundString => "Aging";
    public const int UnlockStage = 999;
}
