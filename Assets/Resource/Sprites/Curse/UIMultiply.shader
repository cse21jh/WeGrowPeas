Shader "UI/Multiply"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1, 1, 1, 1)

        // UI Mask 지원
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
         * 최종 RGB:
         * 배경색 × lerp(1, 스프라이트색, 스프라이트 알파)
         *
         * 최종 Alpha:
         * 기존 렌더 타깃의 알파 유지
         */
        Blend DstColor OneMinusSrcAlpha, Zero One

        ColorMask [_ColorMask]

        Pass
        {
            Name "UIMultiply"

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
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4 color       : COLOR;

                // RectMask2D 클리핑 계산용 로컬 좌표
                float2 localPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
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

                // Image 컴포넌트 Color × 머티리얼 Tint
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
                    step(
                        clipRect.xy,
                        position
                    );

                half2 insideMax =
                    step(
                        position,
                        clipRect.zw
                    );

                return
                    insideMin.x *
                    insideMin.y *
                    insideMax.x *
                    insideMax.y;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                half4 color =
                    textureColor *
                    input.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= GetRectClipFactor(
                    input.localPosition,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001h);
                #endif

                /*
                 * Blend DstColor OneMinusSrcAlpha 계산:
                 *
                 * 결과 =
                 * (source.rgb × destination.rgb)
                 * + destination.rgb × (1 - source.a)
                 *
                 * source.rgb에 알파를 미리 곱해
                 * 투명한 영역은 배경에 영향을 주지 않도록 처리
                 */
                half3 multiplyColor =
                    color.rgb * color.a;

                return half4(
                    multiplyColor,
                    color.a
                );
            }

            ENDHLSL
        }
    }
}
