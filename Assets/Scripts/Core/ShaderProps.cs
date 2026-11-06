using UnityEngine;

/// <summary>셰이더 프로퍼티 이름. 셰이더의 이름과 같아야 한다.</summary>
public static class ShaderProps
{
    public static readonly int BackRect = Shader.PropertyToID("_BackRect");
    public static readonly int BackTex = Shader.PropertyToID("_BackTex");
    public static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
    public static readonly int Dryness = Shader.PropertyToID("_Dryness");
    public static readonly int FrontRect = Shader.PropertyToID("_FrontRect");
    public static readonly int MeltStrength = Shader.PropertyToID("_MeltStrength");
    public static readonly int Radius = Shader.PropertyToID("_Radius");
}
