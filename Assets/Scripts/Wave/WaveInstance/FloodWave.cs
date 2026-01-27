using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloodWave : Wave
{
    public override WaveType WaveType => WaveType.Flood;
    public override string WaveName => "홍수";
    public override string WaveDescription => "홍수가\n덮쳐옵니다......";
    public override string WaveSoundString => "Flood";
    public const int UnlockStage = 16;
}
