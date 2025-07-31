using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CompleteTraitType // 기존 형질
{
    NaturalDeath,
    WindResistance,
    FloodResistance,
    PestResistance,
    ColdResistance,
    HeavyRainResistance,
    None
}

[System.Serializable]
public struct GeneticTrait
{
    public CompleteTraitType traitType;
    public float resistance;
    public int genetics;
    public float additionalResistance;

    public GeneticTrait(CompleteTraitType type, float resistance, int genetics, float addR)
    {
        this.traitType = type;
        this.resistance = resistance;
        this.genetics = genetics;
        this.additionalResistance = addR;
    }
}