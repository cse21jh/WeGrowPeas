// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using LeTai.Common;
using UnityEngine;

namespace LeTai.Paraform
{
[Serializable]
public struct FormAssetGenerationParams
{
    public Texture2D source;

    [Min(8), Max(2048)]
    public int resolution;

    [Tooltip("Extra texture pixels around the form for effects")]
    [Min(0)]
    public int padding;

    [Tooltip("Limit the maximum stored distance, which limits effects like ring or bevel width, but gain more floating point accuracy")]
    [Min(8), Max(2048)]
    public float distanceRange;

    // public int   nContourSmoothIter      = 4;
    // public float nContourSmoothAmplitude = .5f;
    // public float nContourSmoothRecover   = .03f;

    [Range(1, 32)]
    public float contourSmooth;
    // public float sigmaGrowth    = 1.5f;

    [Range(0, 32)]
    public float interiorSmooth;

    [Fold("Advanced")]
    [Range(0, 1)]
    [Tooltip("Slightly expand the shape to repair disconnected segments from thin features. Use only the minimum needed")]
    public float contourShift;

    [Min(0), Max(180)]
    public float bendAngleCutoff;
    [Min(0)]
    public float fitErrorCutoff;

    public static FormAssetGenerationParams Default => new() {
        resolution      = 256,
        distanceRange   = 182,
        contourSmooth   = 9f,
        bendAngleCutoff = 35f,
        fitErrorCutoff  = 3f,
        interiorSmooth  = 5f
    };
}
}
