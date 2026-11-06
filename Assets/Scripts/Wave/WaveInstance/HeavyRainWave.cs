using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeavyRainWave : Wave
{
    public override WaveType WaveType => WaveType.HeavyRain;
    public override string WaveDescription => Loc.T("game.wave.HeavyRain.description");
    public override string WaveSoundString => SoundIds.HeavyRain;
    public const int UnlockStage = 21;
}
