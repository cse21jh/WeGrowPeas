Shader "SpriteGlow"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1, 1, 1, 1)

        [HDR]
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)

        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
        _GlowSize ("Glow Size", Range(0, 10)) = 2
        _GlowSoftness ("Glow Softness", Range(0, 1)) = 0.5
        _SpriteEmission ("Sprite Emission", Range(0, 5)) = 0.5

        // SpriteRenderer 내부 프로퍼티
        [HideInInspector]
        _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)

        [HideInInspector]
        _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        // Premultiplied Alpha
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "SpriteGlow"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

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
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;

                half _GlowIntensity;
                half _GlowSize;
                half _GlowSoftness;
                half _SpriteEmission;
            CBUFFER_END

            // SpriteRenderer가 자동으로 설정하는 값
            half4 _RendererColor;
            float4 _Flip;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // SpriteRenderer의 Flip X/Y 지원
                input.positionOS.xy *= _Flip.xy;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                output.color =
                    input.color *
                    _Color *
                    _RendererColor;

                return output;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                ).a;
            }

            half SampleMaxAlpha8(float2 uv, float2 offset)
            {
                float2 diagonal = offset * 0.70710678;

                half result = 0;

                // 상하좌우
                result = max(
                    result,
                    SampleAlpha(uv + float2(offset.x, 0))
                );

                result = max(
                    result,
                    SampleAlpha(uv - float2(offset.x, 0))
                );

                result = max(
                    result,
                    SampleAlpha(uv + float2(0, offset.y))
                );

                result = max(
                    result,
                    SampleAlpha(uv - float2(0, offset.y))
                );

                // 대각선
                result = max(
                    result,
                    SampleAlpha(
                        uv + float2(diagonal.x, diagonal.y)
                    )
                );

                result = max(
                    result,
                    SampleAlpha(
                        uv + float2(-diagonal.x, diagonal.y)
                    )
                );

                result = max(
                    result,
                    SampleAlpha(
                        uv + float2(diagonal.x, -diagonal.y)
                    )
                );

                result = max(
                    result,
                    SampleAlpha(
                        uv - float2(diagonal.x, diagonal.y)
                    )
                );

                return result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                half4 spriteColor =
                    textureColor * input.color;

                half spriteAlpha = spriteColor.a;

                float2 glowOffset =
                    _MainTex_TexelSize.xy *
                    max(_GlowSize, 0.001);

                half nearAlpha =
                    SampleMaxAlpha8(
                        input.uv,
                        glowOffset
                    );

                half farAlpha =
                    SampleMaxAlpha8(
                        input.uv,
                        glowOffset * 2.0
                    );

                // Softness가 높을수록 더 먼 샘플까지 섞음
                half surroundingAlpha =
                    lerp(
                        nearAlpha,
                        (nearAlpha + farAlpha) * 0.5,
                        _GlowSoftness
                    );

                // 스프라이트 바깥쪽에만 글로우 생성
                half glowMask =
                    saturate(
                        surroundingAlpha -
                        textureColor.a
                    );

                half glowAlpha =
                    saturate(
                        glowMask *
                        _GlowColor.a *
                        input.color.a
                    );

                // Premultiplied Alpha 방식의 원본 색상
                half3 baseColor =
                    spriteColor.rgb *
                    spriteAlpha;

                // 원본 스프라이트 자체를 밝게 만들어 Bloom에 반응시킴
                half3 spriteEmission =
                    spriteColor.rgb *
                    spriteAlpha *
                    _SpriteEmission;

                // 외곽 글로우
                half3 glowColor =
                    _GlowColor.rgb *
                    _GlowIntensity *
                    glowAlpha;

                half finalAlpha =
                    spriteAlpha +
                    glowAlpha *
                    (1.0h - spriteAlpha);

                half3 finalColor =
                    baseColor +
                    spriteEmission +
                    glowColor;

                return half4(
                    finalColor,
                    saturate(finalAlpha)
                );
            }

            ENDHLSL
        }
    }
}
