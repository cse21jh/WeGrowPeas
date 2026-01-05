using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType
{
    Aging,
    Pest,
    Wind,
    Flood,
    HeavyRain,
    Cold,
    Drought,
    Heat,
    None
}

public abstract class Wave
{
    public virtual WaveType WaveType=> WaveType.None;
    public virtual string WaveName => null;
    public virtual string WaveDescription => null;
    public virtual string WaveSoundString => null;
    
    public static int NumberOfWave = 8;
}
