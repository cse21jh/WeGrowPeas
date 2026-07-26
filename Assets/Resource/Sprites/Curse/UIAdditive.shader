Shader "UI/Additive"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [HDR]
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _Intensity ("Intensity", Range(0, 8)) = 1

        /*
         * 0: 일반 Additive 이미지
         *    투명 배경 + 밝은 무늬
         *
         * 1: 반전 마스크 이미지
         *    흰 배경 + 검거나 어두운 무늬
         */
        _InvertLuminance ("Invert Luminance", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseAlphaClip ("Use Alpha Clip", Float) = 0
    }

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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        /*
         * RGB는 Additive로 합산합니다.
         * Alpha는 기존 렌더 타깃 값을 유지합니다.
         *
         * UI 파티클이 겹칠 때 Canvas의 알파 채널까지
         * 계속 누적되는 현상을 방지합니다.
         */
        Blend One One, Zero One

        ColorMask [_ColorMask]

        Pass
        {
            Name "UIAdditive"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                half4 color          : COLOR;
                float2 localPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
                half _InvertLuminance;
                float4 _ClipRect;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    input.uv * _MainTex_ST.xy +
                    _MainTex_ST.zw;

                // Image Color × Material Tint
                output.color =
                    input.color * _Color;

                output.localPosition =
                    input.positionOS.xy;

                return output;
            }

            half GetRectClipFactor(
                float2 position,
                float4 clipRect
            )
            {
                half2 insideMin =
                    step(clipRect.xy, position);

                half2 insideMax =
                    step(position, clipRect.zw);

                return
                    insideMin.x *
                    insideMin.y *
                    insideMax.x *
                    insideMax.y;
            }

            half GetLuminance(half3 color)
            {
                return dot(
                    color,
                    half3(0.2126h, 0.7152h, 0.0722h)
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                half clipFactor = 1.0h;

                #ifdef UNITY_UI_CLIP_RECT
                clipFactor =
                    GetRectClipFactor(
                        input.localPosition,
                        _ClipRect
                    );
                #endif

                /*
                 * 일반 모드:
                 * 텍스처의 기존 RGB와 Alpha를 사용합니다.
                 */
                half regularMask =
                    tex.a *
                    input.color.a *
                    clipFactor;

                half3 regularColor =
                    tex.rgb *
                    input.color.rgb *
                    regularMask;

                /*
                 * 반전 모드:
                 *
                 * 흰색   → 마스크 0
                 * 회색   → 중간 강도
                 * 검은색 → 마스크 1
                 */
                half luminance =
                    GetLuminance(tex.rgb);

                half invertedMask =
                    (1.0h - luminance) *
                    tex.a *
                    input.color.a *
                    clipFactor;

                half3 invertedColor =
                    input.color.rgb *
                    invertedMask;

                half effectMask =
                    lerp(
                        regularMask,
                        invertedMask,
                        _InvertLuminance
                    );

                half3 finalColor =
                    lerp(
                        regularColor,
                        invertedColor,
                        _InvertLuminance
                    );

                finalColor *= _Intensity;

                #ifdef UNITY_UI_ALPHACLIP
                clip(effectMask - 0.001h);
                #endif

                return half4(
                    finalColor,
                    effectMask
                );
            }

            ENDHLSL
        }
    }
}
