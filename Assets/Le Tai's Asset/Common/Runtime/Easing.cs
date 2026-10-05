using System;
using UnityEngine;

namespace LeTai.Common
{
public enum Ease
{
    Linear       = 0,
    QuadIn       = 2,
    QuadOut      = 3,
    QuadInOut    = 4,
    CubicIn      = 6,
    CubicOut     = 7,
    CubicInOut   = 8,
    PolyIn       = 10,
    PolyOut      = 11,
    PolyInOut    = 12,
    SinIn        = 14,
    SinOut       = 15,
    SinInOut     = 16,
    ExpIn        = 18,
    ExpOut       = 19,
    ExpInOut     = 20,
    CircleIn     = 22,
    CircleOut    = 23,
    CircleInOut  = 24,
    BounceIn     = 26,
    BounceOut    = 27,
    BounceInOut  = 28,
    BackIn       = 30,
    BackOut      = 31,
    BackInOut    = 32,
    ElasticIn    = 34,
    ElasticOut   = 35,
    ElasticInOut = 36
}

//https://github.com/d3/d3-ease
public static class Easing
{
    const           float BACK_OVERSHOOT        = 1.70158f;
    const           float ELASTIC_AMPLITUDE     = 1f;
    const           float ELASTIC_PERIOD        = .3f;
    const           float BOUNCE_B1             = 4f / 11f;
    const           float BOUNCE_B2             = 6f / 11f;
    const           float BOUNCE_B3             = 8f / 11f;
    const           float BOUNCE_B4             = 3f / 4f;
    const           float BOUNCE_B5             = 9f / 11f;
    const           float BOUNCE_B6             = 10f / 11f;
    const           float BOUNCE_B7             = 15f / 16f;
    const           float BOUNCE_B8             = 21f / 22f;
    const           float BOUNCE_B9             = 63f / 64f;
    const           float BOUNCE_B0             = 1f / BOUNCE_B1 / BOUNCE_B1;
    const           float ELASTIC_TAU           = 2f * Mathf.PI;
    static readonly float ELASTIC_PHASE         = Mathf.Asin(1f / ELASTIC_AMPLITUDE) * (ELASTIC_PERIOD / ELASTIC_TAU);
    const           float ELASTIC_SCALED_PERIOD = ELASTIC_PERIOD / ELASTIC_TAU;

    public static float Eval(Ease ease, float progress) => ease switch {
        Ease.Linear       => progress,
        Ease.QuadIn       => QuadIn(progress),
        Ease.QuadOut      => QuadOut(progress),
        Ease.QuadInOut    => QuadInOut(progress),
        Ease.CubicIn      => CubicIn(progress),
        Ease.CubicOut     => CubicOut(progress),
        Ease.CubicInOut   => CubicInOut(progress),
        Ease.PolyIn       => PolyIn(progress),
        Ease.PolyOut      => PolyOut(progress),
        Ease.PolyInOut    => PolyInOut(progress),
        Ease.SinIn        => SinIn(progress),
        Ease.SinOut       => SinOut(progress),
        Ease.SinInOut     => SinInOut(progress),
        Ease.ExpIn        => ExpIn(progress),
        Ease.ExpOut       => ExpOut(progress),
        Ease.ExpInOut     => ExpInOut(progress),
        Ease.CircleIn     => CircleIn(progress),
        Ease.CircleOut    => CircleOut(progress),
        Ease.CircleInOut  => CircleInOut(progress),
        Ease.BounceIn     => BounceIn(progress),
        Ease.BounceOut    => BounceOut(progress),
        Ease.BounceInOut  => BounceInOut(progress),
        Ease.BackIn       => BackIn(progress),
        Ease.BackOut      => BackOut(progress),
        Ease.BackInOut    => BackInOut(progress),
        Ease.ElasticIn    => ElasticIn(progress),
        Ease.ElasticOut   => ElasticOut(progress),
        Ease.ElasticInOut => ElasticInOut(progress),
        _                 => throw new ArgumentOutOfRangeException(nameof(ease), ease, null)
    };

    static float Tpmt(float value) => (Mathf.Pow(2f, -10f * value) - .0009765625f) * 1.0009775171065494f;

    public static float QuadIn(float  progress) => progress * progress;
    public static float QuadOut(float progress) => progress * (2f - progress);

    public static float QuadInOut(float progress) =>
        ((progress *= 2f) <= 1f ? progress * progress : --progress * (2f - progress) + 1f) / 2f;

    public static float CubicIn(float  progress) => progress * progress * progress;
    public static float CubicOut(float progress) => --progress * progress * progress + 1f;

    public static float CubicInOut(float progress) =>
        ((progress *= 2f) <= 1f ? progress * progress * progress : (progress -= 2f) * progress * progress + 2f) / 2f;

    public static float PolyIn(float    progress)                 => PolyIn(progress, 3f);
    public static float PolyIn(float    progress, float exponent) => Mathf.Pow(progress, exponent);
    public static float PolyOut(float   progress)                 => PolyOut(progress, 3f);
    public static float PolyOut(float   progress, float exponent) => 1f - Mathf.Pow(1f - progress, exponent);
    public static float PolyInOut(float progress) => PolyInOut(progress, 3f);

    public static float PolyInOut(float progress, float exponent) =>
        ((progress *= 2f) <= 1f
            ? Mathf.Pow(progress, exponent)
            : 2f - Mathf.Pow(2f - progress, exponent)) / 2f;

    public static float SinIn(float    progress) => progress == 1f ? 1f : 1f - Mathf.Cos(progress * Mathf.PI / 2f);
    public static float SinOut(float   progress) => Mathf.Sin(progress * Mathf.PI / 2f);
    public static float SinInOut(float progress) => (1f - Mathf.Cos(Mathf.PI * progress)) / 2f;

    public static float ExpIn(float  progress) => Tpmt(1f - progress);
    public static float ExpOut(float progress) => 1f - Tpmt(progress);

    public static float ExpInOut(float progress) => ((progress *= 2f) <= 1f ? Tpmt(1f - progress) : 2f - Tpmt(progress - 1f)) / 2f;

    public static float CircleIn(float  progress) => 1f - Mathf.Sqrt(1f - progress * progress);
    public static float CircleOut(float progress) => Mathf.Sqrt(1f - (--progress) * progress);

    public static float CircleInOut(float progress) =>
        ((progress *= 2f) <= 1f
            ? 1f - Mathf.Sqrt(1f - progress * progress)
            : Mathf.Sqrt(1f - (progress -= 2f) * progress) + 1f) / 2f;

    public static float BounceIn(float progress) => 1f - BounceOut(1f - progress);

    public static float BounceOut(float progress)
    {
        if (progress < BOUNCE_B1)
            return BOUNCE_B0 * progress * progress;
        if (progress < BOUNCE_B3)
            return BOUNCE_B0 * (progress - BOUNCE_B2) * (progress - BOUNCE_B2) + BOUNCE_B4;
        if (progress < BOUNCE_B6)
            return BOUNCE_B0 * (progress - BOUNCE_B5) * (progress - BOUNCE_B5) + BOUNCE_B7;
        return BOUNCE_B0 * (progress - BOUNCE_B8) * (progress - BOUNCE_B8) + BOUNCE_B9;
    }

    public static float BounceInOut(float progress) =>
        ((progress *= 2f) <= 1f ? 1f - BounceOut(1f - progress) : BounceOut(progress - 1f) + 1f) / 2f;

    public static float BackIn(float progress) => BackIn(progress, BACK_OVERSHOOT);

    public static float BackIn(float progress, float overshoot) =>
        progress * progress * (overshoot * (progress - 1f) + progress);

    public static float BackOut(float progress) => BackOut(progress, BACK_OVERSHOOT);

    public static float BackOut(float progress, float overshoot) =>
        --progress * progress * ((progress + 1f) * overshoot + progress) + 1f;

    public static float BackInOut(float progress) => BackInOut(progress, BACK_OVERSHOOT);

    public static float BackInOut(float progress, float overshoot)
    {
        var scaledProgress = progress * 2f;
        return (scaledProgress < 1f
            ? scaledProgress * scaledProgress * ((overshoot + 1f) * scaledProgress - overshoot)
            : (scaledProgress -= 2f) * scaledProgress * ((overshoot + 1f) * scaledProgress + overshoot) + 2f) / 2f;
    }

    public static float ElasticIn(float progress) => ElasticIn(progress, ELASTIC_AMPLITUDE, ELASTIC_PHASE, ELASTIC_SCALED_PERIOD);

    public static float ElasticIn(float progress, float amplitude, float period)
    {
        amplitude = Math.Max(1f, amplitude);
        var scaledPeriod = period / ELASTIC_TAU;
        var phase        = Mathf.Asin(1f / amplitude) * scaledPeriod;
        return ElasticIn(progress, amplitude, phase, scaledPeriod);
    }

    static float ElasticIn(float progress, float amplitude, float phase, float scaledPeriod)
    {
        progress = 1f - progress;
        return amplitude * Tpmt(progress) * Mathf.Sin((phase + progress) / scaledPeriod);
    }

    public static float ElasticOut(float progress) => ElasticOut(progress, ELASTIC_AMPLITUDE, ELASTIC_PHASE, ELASTIC_SCALED_PERIOD);

    public static float ElasticOut(float progress, float amplitude, float period)
    {
        amplitude = Math.Max(1f, amplitude);
        var scaledPeriod = period / ELASTIC_TAU;
        var phase        = Mathf.Asin(1f / amplitude) * scaledPeriod;
        return ElasticOut(progress, amplitude, phase, scaledPeriod);
    }

    static float ElasticOut(float progress, float amplitude, float phase, float scaledPeriod) =>
        1f - amplitude * Tpmt(progress) * Mathf.Sin((progress + phase) / scaledPeriod);

    public static float ElasticInOut(float progress) => ElasticInOut(progress, ELASTIC_AMPLITUDE, ELASTIC_PHASE, ELASTIC_SCALED_PERIOD);

    public static float ElasticInOut(float progress, float amplitude, float period)
    {
        amplitude = Math.Max(1f, amplitude);
        var scaledPeriod = period / ELASTIC_TAU;
        var phase        = Mathf.Asin(1f / amplitude) * scaledPeriod;
        return ElasticInOut(progress, amplitude, phase, scaledPeriod);
    }

    static float ElasticInOut(float progress, float amplitude, float phase, float scaledPeriod)
    {
        progress = progress * 2f - 1f;
        return (progress < 0f
            ? amplitude * Tpmt(-progress) * Mathf.Sin((phase - progress) / scaledPeriod)
            : 2f - amplitude * Tpmt(progress) * Mathf.Sin((phase + progress) / scaledPeriod)) / 2f;
    }
}
}
