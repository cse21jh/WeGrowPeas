using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodWave : Wave
{
    public override WaveType WaveType => WaveType.Flood;
    public override string WaveDescription => Loc.T("game.wave.Flood.description");
    public override string WaveSoundString => SoundIds.Flood;
    public const int UnlockStage = 16;
}
