// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System.Collections.Generic;
using System.Diagnostics;
using LeTai.Common;
using LeTai.Paraform;
using LeTai.Paraform.Scaffold;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace LeTai.Asset.TranslucentImage
{
public partial class TranslucentImage : IParaformHost
{
    public ParaformConfig paraformConfig = ParaformConfig.DEFAULT;

    ref ParaformConfig IParaformHost.ParaformConfig => ref paraformConfig;

    [Conditional("LETAI_PARAFORM")]
    void IncludeParaformEffectsInGeometryBounds(ref bool hasExpandedGeometry)
    {
#if LETAI_PARAFORM
        hasExpandedGeometry |= IsParaform() &&
                               paraformConfig.Effects.Count > 0;
#endif
    }

    [Conditional("LETAI_PARAFORM")]
    static void PrepareParaformEffectShadowVertex(ref UIVertex vertex, ref bool useStandardShadowEncoding)
    {
#if LETAI_PARAFORM
        if (vertex.uv3.y >= 0)
            return;

        useStandardShadowEncoding = false;
#endif
    }

#if LETAI_PARAFORM
    void PopulateParaformEffects(VertexHelper vh, Rect rect, ParaformVertexDataEncoder encoder)
    {
        if (!IsParaform())
            return;

        var effects = paraformConfig.Effects;
        if (effects.Count == 0)
            return;

        var pixelSize = canvas.renderMode == RenderMode.WorldSpace
            ? Mathf.Min(2f, Mathf.Min(rect.width, rect.height) * .02f)
            : 1f / canvas.scaleFactor;
        ParaformEffectMesh.Populate(vh, rect, in paraformConfig, encoder, effects, pixelSize, color);
    }
#endif

    const string PARAFORM_SHADER_NAME = "UI/TranslucentImage-Paraform";

    float  _prevScale = 0;
    Shader _prevShader;
    bool   _wasParaform;

    static readonly List<TranslucentImage> TRACKED_TEXTURELESS_IMAGES = new();
    static          bool                   _isAtlasTrackingInitialized;

    bool _isTrackedTextureless;

    bool IsParaform()
    {
        var mat    = material;
        var shader = mat ? mat.shader : null;

        if (!ReferenceEquals(shader, _prevShader)) // cheaper. don't need null check
        {
            _prevShader = shader;
            // .name allocs. strcmp is not so fast. But shader object ref compare may fail due to asset bundle dup :(
            // at least this should only run occasionally
            _wasParaform = shader && shader.name == PARAFORM_SHADER_NAME;
        }

        return _wasParaform;
    }

    public FormAsset FormAsset
    {
        get => paraformConfig.FormAsset;
        set => paraformConfig.FormAsset = value;
    }

    [Conditional("LETAI_PARAFORM")]
    void TrackFormSprite()
    {
        var formSprite = GetFormSprite();
        if (formSprite && !formSprite.texture)
        {
            TrackTexturelessImage(this);
            _isTrackedTextureless = true;
        }
    }

    [Conditional("LETAI_PARAFORM")]
    void UnTrackFormSprite()
    {
        if (!_isTrackedTextureless)
            return;

        TRACKED_TEXTURELESS_IMAGES.Remove(this);
        _isTrackedTextureless = false;
    }

    [Conditional("LETAI_PARAFORM")]
    void RefreshFormSpriteTracking()
    {
        UnTrackFormSprite();
        if (isActiveAndEnabled)
            TrackFormSprite();
    }

    Sprite GetFormSprite()
    {
#if LETAI_PARAFORM
        if (IsParaform())
            return paraformConfig.FormSprite;
#endif

        return null;
    }

    static void TrackTexturelessImage(TranslucentImage image)
    {
        if (!_isAtlasTrackingInitialized)
        {
            SpriteAtlasManager.atlasRegistered += RebuildTexturelessImages;
            _isAtlasTrackingInitialized        =  true;
        }

        TRACKED_TEXTURELESS_IMAGES.Add(image);
    }

    static void RebuildTexturelessImages(SpriteAtlas atlas)
    {
        for (var i = TRACKED_TEXTURELESS_IMAGES.Count - 1; i >= 0; --i)
        {
            var image      = TRACKED_TEXTURELESS_IMAGES[i];
            var formSprite = image.GetFormSprite();
            if (formSprite && atlas.CanBindTo(formSprite))
            {
                image.SetAllDirty();
                TRACKED_TEXTURELESS_IMAGES.RemoveAt(i);
            }
        }
    }

    [Conditional("LETAI_PARAFORM")]
    private void PadRectForRefraction(ref Rect rect)
    {
#if LETAI_PARAFORM
        if (!IsParaform())
            return;

        rect = ParaformUtils.GetRefractionRect(in paraformConfig,
                                               rect,
                                               material.GetVector(ShaderID.REFRACTIVE_INDEX_RATIOS));
#endif
    }

    [Conditional("LETAI_PARAFORM")]
    void SetParaformShaderGlobal()
    {
        if (!canvas) // trigger on undo
            return;

        Shader.SetGlobalFloat(ShaderID.G_CANVAS_SCALE_FACTOR, canvas.scaleFactor);
    }

    [Conditional("LETAI_PARAFORM")]
    void LateUpdate()
    {
        var localScale = rectTransform.localScale;
        // var scale      = (localScale.x + localScale.y) / 2f;
        var scale = Mathf.Max(localScale.x, localScale.y);
        if (Mathf.Abs(scale - _prevScale) > 1e-5f)
        {
            SetVerticesDirty();
            _prevScale = scale;
        }
    }

    protected override void UpdateMaterial()
    {
        base.UpdateMaterial();

        var paraformTexture = GetParaformTexture();
        if (paraformTexture)
            canvasRenderer.SetTexture(paraformTexture);
    }

    Texture GetParaformTexture()
    {
        var formSprite = GetFormSprite();
        return formSprite ? formSprite.texture : null;
    }

    [Conditional("LETAI_PARAFORM")]
    public static void CopyParaformMaterialPropertiesTo(Material src, Material dst)
    {
        MaterialUtils.CopyKeyword(src, dst, ShaderID.REFRACTION_MODE_OFF);
        MaterialUtils.CopyKeyword(src, dst, ShaderID.REFRACTION_MODE_ON);
        MaterialUtils.CopyKeyword(src, dst, ShaderID.REFRACTION_MODE_CHROMATIC);
        MaterialUtils.CopyKeyword(src, dst, ShaderID.USE_EDGE_GLINT);

        MaterialUtils.CopyFloat(src, dst, ShaderID.REFRACTIVE_INDEX_DUMMY);
        MaterialUtils.CopyFloat(src, dst, ShaderID.CHROMATIC_DISPERSION_DUMMY);
        MaterialUtils.CopyVector(src, dst, ShaderID.REFRACTIVE_INDEX_RATIOS);

        MaterialUtils.CopyVector(src, dst, ShaderID.EDGE_GLINT_DIRECTIONS);
        MaterialUtils.CopyColor(src, dst, ShaderID.EDGE_GLINT1_COLOR);
        MaterialUtils.CopyColor(src, dst, ShaderID.EDGE_GLINT2_COLOR);
        MaterialUtils.CopyFloat(src, dst, ShaderID.EDGE_GLINT_WRAP_RAW);
        MaterialUtils.CopyFloat(src, dst, ShaderID.EDGE_GLINT_SHARPNESS_RAW);
    }

#if LETAI_PARAFORM
    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera))
            return false;

        if (!IsParaform())
            return true;

        if (!RectTransformUtilityPatch.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
            return false;

        var rect = paraformConfig.FormMode == FormMode.Asset ? GetPixelAdjustedRect() : rectTransform.rect;
        return ParaformUtils.IsRaycastLocationValid(paraformConfig, rect, local, rectTransform.pivot, raycastPadding);
    }

    protected override void OnDidApplyAnimationProperties()
    {
        SetVerticesDirty();
        base.OnDidApplyAnimationProperties();
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ParaformMaterial.SetDispersion"/>.
    /// For better performance, use that instead and manage the material yourself.
    /// </summary>
    public void SetDispersionSlow(float dispersion)
    {
        ParaformMaterial.SetDispersion(materialForRendering, dispersion);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ParaformMaterial.SetRefractiveIndex"/>.
    /// For better performance, use that instead and manage the material yourself.
    /// </summary>
    public void SetRefractiveIndexSlow(float refractiveIndex)
    {
        ParaformMaterial.SetRefractiveIndex(materialForRendering, refractiveIndex);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ParaformMaterial.SetRefractiveIndexRatios"/>.
    /// For better performance, use that instead and manage the material yourself.
    /// </summary>
    public void SetRefractiveIndexRatiosSlow(float refractiveIndex, float chromaticDispersion)
    {
        ParaformMaterial.SetRefractiveIndexRatios(materialForRendering, refractiveIndex, chromaticDispersion);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ParaformMaterial.SetEdgeGlintWrap"/>.
    /// For better performance, use that instead and manage the material yourself.
    /// </summary>
    public void SetEdgeGlintWrap(float edgeGlintWrapNormalized)
    {
        ParaformMaterial.SetEdgeGlintWrap(materialForRendering, edgeGlintWrapNormalized);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="ParaformMaterial.SetEdgeGlintSharpness"/>.
    /// For better performance, use that instead and manage the material yourself.
    /// </summary>
    public void SetEdgeGlintSharpness(float edgeGlintSharpnessNormalized)
    {
        ParaformMaterial.SetEdgeGlintSharpness(materialForRendering, edgeGlintSharpnessNormalized);
    }
#endif
}
}
