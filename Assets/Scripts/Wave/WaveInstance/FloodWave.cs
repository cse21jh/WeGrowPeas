using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodWave : Wave
{
    public override WaveType WaveType => WaveType.Flood;
    public override string WaveName => "È«¼ö";
    public override string WaveDescription => "È«¼ö°¡ µ¤ÃÄ¿É´Ï´Ù......";
    public override string WaveSoundString => "Flood";
    public const int UnlockStage = 10;
}
