Shader "Sprite/Multiply"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [MainColor]
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _MultiplyStrength (
            "Multiply Strength",
            Range(0.1, 4)
        ) = 1

        [HideInInspector]
        _RendererColor (
            "Renderer Color",
            Color
        ) = (1, 1, 1, 1)

        [HideInInspector]
        _Flip (
            "Flip",
            Vector
        ) = (1, 1, 1, 1)
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        /*
         * RGB:
         * 결과 = 기존 화면색 × 셰이더 출력색
         *
         * Alpha:
         * 기존 렌더 타깃 알파 유지
         */
        Blend DstColor Zero, Zero One

        Pass
        {
            Name "SpriteMultiply"

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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _MultiplyStrength;
            CBUFFER_END

            half4 _RendererColor;
            float4 _Flip;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // SpriteRenderer의 Flip X/Y 지원
                input.positionOS.xy *= _Flip.xy;

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv = input.uv;

                output.color =
                    input.color *
                    _Color *
                    _RendererColor;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                half4 spriteColor =
                    tex * input.color;

                half alpha =
                    saturate(spriteColor.a);

                /*
                 * Strength:
                 *
                 * 1보다 높음 → 중간색이 더 어두워짐
                 * 1           → 원본 Multiply
                 * 1보다 낮음 → 효과가 약해짐
                 */
                half3 strengthenedColor =
                    pow(
                        max(
                            saturate(spriteColor.rgb),
                            half3(
                                0.0001h,
                                0.0001h,
                                0.0001h
                            )
                        ),
                        _MultiplyStrength
                    );

                /*
                 * 투명한 픽셀은 반드시 흰색을 출력합니다.
                 *
                 * alpha 0:
                 * multiplier = 1
                 * 배경 × 1 = 변화 없음
                 *
                 * alpha 1:
                 * multiplier = 원본 색상
                 */
                half3 multiplier =
                    lerp(
                        half3(1.0h, 1.0h, 1.0h),
                        strengthenedColor,
                        alpha
                    );

                return half4(
                    multiplier,
                    1.0h
                );
            }

            ENDHLSL
        }
    }
}
