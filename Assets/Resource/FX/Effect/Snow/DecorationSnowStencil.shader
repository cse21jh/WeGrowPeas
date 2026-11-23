// 장식품이 겨울 눈 덮개(winter_snow/Snow)에 덮이지 않게 하는 스텐실 기록용 셰이더. 화면에는 아무것도 그리지 않는다.
//
// DecorationLayer가 장식품마다 이 머티리얼을 쓴 SpriteRenderer를 자식으로 붙이고, 장식품과 같은 정렬 레이어·순서로 그린다.
//   1) 장식품 뒤쪽 풀 줄은 이것보다 먼저 그려지므로 영향이 없다.
//   2) 이것이 장식품 모양(투명한 곳 제외)에 스텐실 값 1을 기록한다(아래 7비트. 마지막 비트 128은 풀 셰이더가 쓴다).
//   3) 장식품 앞쪽 풀 줄(GrassChunkSwayEffect)은 그 뒤에 그려지며 풀잎 자리의 스텐실을 128로 덮어써 기록을 지운다.
//   4) 눈 덮개는 Mask Interaction = Visible Outside Mask라서 기록이 남은 자리, 즉 실제로 보이는 장식품 픽셀에만 그려지지 않는다.
// SpriteMask를 쓰지 않는 이유: URP 2D에서는 SpriteMask가 정렬 순서와 상관없이 먼저 그려져, 장식품 뒤쪽 풀까지 기록을 지워 버린다.
Shader "Custom/2D/DecorationSnowStencil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0.0, 1.0)) = 0.2
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    half _Cutoff;
    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    struct Attributes
    {
        float4 positionOS : POSITION;
        half2 texcoord : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        half2 uv : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        output.uv = input.texcoord;
        return output;
    }

    half4 Frag(Varyings input) : SV_Target
    {
        half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
        clip(c.a - _Cutoff);
        return 0;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend Off
        ColorMask 0

        Stencil
        {
            Ref 1
            WriteMask 127
            Comp Always
            Pass Replace
        }

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
