using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TraitType // 기존 형질
{
    NaturalDeath,
    Pest,
    Wind,
    Flood,
    HeavyRain,
    Cold,
    Drought,
    Heat,
    None,
    FD,
    CH
}

[System.Serializable]
public struct GeneticTrait
{
    public TraitType traitType;
    public float resistance;
    public int genetics;
    public float additionalResistance;

    public GeneticTrait(TraitType type, float resistance, int genetics, float addR)
    {
        this.traitType = type;
        this.resistance = resistance;
        this.genetics = genetics;
        this.additionalResistance = addR;
    }
}
