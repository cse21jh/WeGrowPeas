#if LETAI_TRUESHADOW

using System;
using LeTai.TrueShadow.PluginInterfaces;
using UnityEngine;

namespace LeTai.Asset.TranslucentImage
{
public partial class TranslucentImage : ITrueShadowCustomHashProviderV2,
                                       ITrueShadowCasterMaterialPropertiesModifier
{
    public event Action<int> trueShadowCustomHashChanged;

    public void ModifyTrueShadowCasterMaterialProperties(MaterialPropertyBlock propertyBlock)
    {
        var spriteTexture = GetParaformTexture();
        if (spriteTexture)
            propertyBlock.SetTexture(ShaderID.MAIN_TEX, spriteTexture);
    }

    partial void UpdateTrueShadowHash()
    {
        var spriteTexture = GetParaformTexture();

        trueShadowCustomHashChanged?.Invoke(
            HashUtils.CombineHashCodes(
                imageMode.GetHashCode(),
                textureAlphaMode.GetHashCode(),
                paraformConfig.FormMode.GetHashCode(),
                paraformConfig.FormAsset ? paraformConfig.FormAsset.GetHashCode() : 0,
                spriteTexture ? spriteTexture.GetHashCode() : 0,
                spriteTexture ? (int)spriteTexture.updateCount : 0,
                paraformConfig.CornerRadii.GetHashCode(),
                (int)(paraformConfig.CornerCurvature * 100),
                (int)(paraformConfig.RingThickness * 100)
            )
        );
    }
}
}
#endif

namespace LeTai.Asset.TranslucentImage
{
public partial class TranslucentImage
{
    partial void UpdateTrueShadowHash();
}
}
