using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgingWave : Wave
{
    public override WaveType WaveType => WaveType.Aging;
    public override string WaveDescription => "°ð ÇÏ·ç°¡ Áö³ª°©´Ï´Ù......";
    public override string WaveSoundString => "Aging";
    public override int UnlockStage => 0;
}
