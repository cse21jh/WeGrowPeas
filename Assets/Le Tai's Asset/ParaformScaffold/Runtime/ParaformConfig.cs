// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LeTai.Paraform.Scaffold
{
public enum FormMode
{
    RoundedBox,
    Asset
}

[Serializable]
public partial struct ParaformConfig
{
    public static readonly ParaformConfig DEFAULT = new ParaformConfig {
        formMode        = FormMode.RoundedBox,
        cornerCurvature = 1.5f,
        filletCurvature = 2.5f,
        bevelWidth      = 64,
        ringThickness   = 0,
        elevation       = 100,
        cornerRadii     = new Vector4(64, 64, 64, 64),
    };

    Action    _changed;
    FormAsset _formAssetPrev;

    public event Action changed
    {
        add
        {
            FormAsset =  formAsset;
            _changed  += value;
            if (_formAssetPrev)
                _formAssetPrev.changed += value;
        }
        remove
        {
            _changed -= value;
            if (_formAssetPrev)
                _formAssetPrev.changed -= value;
        }
    }

    [SerializeField]
    internal FormMode formMode;

    public FormMode FormMode
    {
        get => formMode;
        set => Set(ref formMode, value);
    }

    [SerializeField]
    internal FormAsset formAsset;

    public FormAsset FormAsset
    {
        get => formAsset;
        set
        {
            formAsset = value;
            if (_formAssetPrev == formAsset)
                return;

            if (_formAssetPrev && _changed != null)
                _formAssetPrev.changed -= _changed;
            _formAssetPrev = formAsset;
            if (_formAssetPrev && _changed != null)
                _formAssetPrev.changed += _changed;
            NotifyChanged();
        }
    }

    public readonly Sprite FormSprite => formMode == FormMode.Asset && formAsset ? formAsset.FormSprite : null;

    [SerializeField]
    [Tooltip("Use the form sprite silhouette for UI raycasts")]
    internal bool usePreciseHitTest;

    public bool UsePreciseHitTest
    {
        get => usePreciseHitTest;
        set => usePreciseHitTest = value;
    }

#if UNITY_EDITOR
    [SerializeField]
    internal bool isCornersLinked;
#endif

    [SerializeField]
    [Tooltip("Indiviual corner radii. You can drag the rounded corner symbols to fine tune each radius. Click the link button to keep all corners the same.")]
    internal Vector4 cornerRadii;

    public Vector4 CornerRadii
    {
        get => cornerRadii;
        set => Set(ref cornerRadii, value);
    }

    [SerializeField]
    [Range(0, 6)]
    [Tooltip("0 is a flat diagonal corner. 1 is the perfect circle. Value > 1 increase curvature continuity target: 2 for G2 continuity, 3 for G3, and so on. Note that curvature continuity requires increased transition length, and is not guaranteed if the corner radius is too large compared to side length.")]
    internal float cornerCurvature;

    public float CornerCurvature
    {
        get => cornerCurvature;
        set => Set(ref cornerCurvature, value);
    }

    [SerializeField]
    [Range(0, 6)]
    [Tooltip("0 is a flat diagonal corner. 1 is the perfect circle. Value > 1 increase curvature continuity target: 2 for G2 continuity, 3 for G3, and so on")]
    internal float filletCurvature;

    public float FilletCurvature
    {
        get => filletCurvature;
        set => Set(ref filletCurvature, value);
    }

    [FormerlySerializedAs("edgeWidth")]
    [SerializeField]
    [Min(0)]
    [Tooltip("Bevel width or thickness")]
    internal float bevelWidth;

    public float BevelWidth
    {
        get => bevelWidth;
        set => Set(ref bevelWidth, value);
    }

    [SerializeField]
    [Min(0)]
    [Tooltip("Thickness of the ring shape. 0 to disable")]
    internal float ringThickness;

    public float RingThickness
    {
        get => ringThickness;
        set => Set(ref ringThickness, value);
    }

    [SerializeField]
    [Range(0, 1000)]
    [Tooltip("Distance to the below surface, affecting refraction")]
    internal float elevation;

    public float Elevation
    {
        get => elevation;
        set => Set(ref elevation, value);
    }

    [SerializeReference]
    internal List<Effect> effects;

    public readonly ParaformConfig Snapshot()
    {
        var snapshot = this;
        snapshot._changed       = null;
        snapshot._formAssetPrev = null;
        if (effects != null)
        {
            snapshot.effects = new List<Effect>(effects.Count);
            for (var index = 0; index < effects.Count; index++)
                snapshot.effects.Add(effects[index]?.Clone());
        }

        return snapshot;
    }

    public void CopyParametersFrom(in ParaformConfig source)
    {
        formMode          = source.formMode;
        usePreciseHitTest = source.usePreciseHitTest;
        cornerRadii       = source.cornerRadii;
        cornerCurvature   = source.cornerCurvature;
        filletCurvature   = source.filletCurvature;
        bevelWidth        = source.bevelWidth;
        ringThickness     = source.ringThickness;
        elevation         = source.elevation;
        var assetChanged = _formAssetPrev != source.formAsset;
        FormAsset = source.formAsset;
        if (!assetChanged)
            NotifyChanged();
    }

    /// <summary>
    /// Does not lerp effects
    /// </summary>
    public void Lerp(in ParaformConfig from, in ParaformConfig to, float amount)
    {
        FormMode          = to.formMode;
        FormAsset         = to.formAsset;
        usePreciseHitTest = to.usePreciseHitTest;
        cornerRadii       = Vector4.LerpUnclamped(from.cornerRadii, to.cornerRadii, amount);
        cornerCurvature   = Mathf.LerpUnclamped(from.cornerCurvature, to.cornerCurvature, amount);
        filletCurvature   = Mathf.LerpUnclamped(from.filletCurvature, to.filletCurvature, amount);
        bevelWidth        = Mathf.LerpUnclamped(from.bevelWidth,      to.bevelWidth,      amount);
        ringThickness     = Mathf.LerpUnclamped(from.ringThickness,   to.ringThickness,   amount);
        elevation         = Mathf.LerpUnclamped(from.elevation,       to.elevation,       amount);
        NotifyChanged();
    }

    void Set(ref float field, float value)
    {
        if (!Mathf.Approximately(field, value))
        {
            field = value;
            NotifyChanged();
        }
    }

    void Set<T>(ref T field, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            NotifyChanged();
        }
    }

    public readonly void NotifyChanged()
    {
        _changed?.Invoke();
    }
}
}
