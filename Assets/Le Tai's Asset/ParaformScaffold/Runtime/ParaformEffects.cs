// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeTai.Paraform.Scaffold
{
public enum SoftnessType
{
    Simple,
    Gaussian,
    Light
}

public enum EffectPlacement
{
    Underlay,
    Overlay
}

public enum ShadowQuality
{
    Low,
    High
}

[Serializable]
public abstract class Effect
{
    public abstract EffectPlacement Placement { get; }

    public Effect Clone() => (Effect)MemberwiseClone();

    public virtual void Lerp(Effect from, Effect to, float amount)
    {
        enabled         = to.enabled;
        useGraphicAlpha = to.useGraphicAlpha;
        useGraphicColor = to.useGraphicColor;
        softnessType    = to.softnessType;
        color           = Color.LerpUnclamped(from.color, to.color, amount);
    }

    public string       name;
    public bool         enabled         = true;
    public Color        color           = new(0, 0, 0, .5f);
    public bool         useGraphicAlpha = true;
    public bool         useGraphicColor = true;
    public SoftnessType softnessType    = SoftnessType.Simple;
}

[Serializable]
public sealed class Shadow : Effect
{
    public override EffectPlacement Placement => EffectPlacement.Underlay;

    public Vector2       offset = new(4, -8);
    public float         spread;
    public ShadowQuality quality = ShadowQuality.High;

    [Min(0)]
    public float softness = 32;

    public Shadow()
    {
        color = Color.black;
    }

    public override void Lerp(Effect from, Effect to, float amount)
    {
        base.Lerp(from, to, amount);
        var start  = (Shadow)from;
        var target = (Shadow)to;
        offset   = Vector2.LerpUnclamped(start.offset, target.offset, amount);
        spread   = Mathf.LerpUnclamped(start.spread, target.spread, amount);
        softness = Mathf.LerpUnclamped(start.softness, target.softness, amount);
        quality  = target.quality;
    }
}

[Serializable]
public sealed class Outline : Effect
{
    public override EffectPlacement Placement => EffectPlacement.Overlay;

    public float innerWidth;
    public float outerWidth = 10;

    [Min(0)]
    public float innerSoftness;

    [Min(0)]
    public float outerSoftness;

    public Outline()
    {
        color = Color.white;
    }

    public override void Lerp(Effect from, Effect to, float amount)
    {
        base.Lerp(from, to, amount);
        var start  = (Outline)from;
        var target = (Outline)to;
        innerWidth    = Mathf.LerpUnclamped(start.innerWidth, target.innerWidth, amount);
        outerWidth    = Mathf.LerpUnclamped(start.outerWidth, target.outerWidth, amount);
        innerSoftness = Mathf.LerpUnclamped(start.innerSoftness, target.innerSoftness, amount);
        outerSoftness = Mathf.LerpUnclamped(start.outerSoftness, target.outerSoftness, amount);
    }
}

public partial struct ParaformConfig
{
    /// <summary>
    /// Do not mutate effects from this list. Use <see cref="EditEffects"/> to get the changes to apply correctly
    /// </summary>
    public readonly IReadOnlyList<Effect> Effects => (IReadOnlyList<Effect>)effects ?? Array.Empty<Effect>();

    public EffectEditScope EditEffects()
    {
        effects ??= new List<Effect>();
        return new EffectEditScope(this);
    }

    public readonly ref struct EffectEditScope
    {
        readonly ParaformConfig _config;

        public readonly List<Effect> effects;

        internal EffectEditScope(ParaformConfig config)
        {
            _config = config;
            effects = config.effects;
        }

        public void Dispose()
        {
            _config.NotifyChanged();
        }
    }
}
}
