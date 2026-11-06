using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroughtWave : Wave
{
    public override WaveType WaveType => WaveType.Drought;
    public override string WaveDescription => Loc.T("game.wave.Drought.description");
    public override string WaveSoundString => SoundIds.Drought;
    public const int UnlockStage = 21;
}
