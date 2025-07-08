using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType
{
    Aging,
    Wind,
    Flood,
    Pest,
    Cold,
    HeavyRain,
    None
}

public abstract class Wave
{
    public virtual WaveType WaveType=> WaveType.None;
    public virtual string WaveDescription => null;
    public virtual string WaveSoundString => null;
    public virtual int UnlockStage => 0;

}
