using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgingWave : Wave
{
    public override WaveType WaveType => WaveType.Aging;
    public override string WaveDescription => Loc.T("game.wave.Aging.description");
    public override string WaveSoundString => SoundIds.Aging;
    public const int UnlockStage = 1;
}
