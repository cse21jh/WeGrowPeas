Shader "UI/ScrollingStaticNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}

        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseTiling ("Noise Tiling", Vector) = (3,3,0,0)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.3,0.7,0,0)

        _FrameRate ("Noise Frame Rate", Range(1,60)) = 24
        _Contrast ("Contrast", Range(0.1,5)) = 2
        _Brightness ("Brightness", Range(-1,1)) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        _JumpStrength ("Random Jump Strength", Range(0,1)) = 1
        _SecondLayerStrength ("Second Layer", Range(0,1)) = 0.35

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            fixed4 _Color;
            float4 _ClipRect;

            float4 _NoiseTiling;
            float4 _ScrollSpeed;

            float _FrameRate;
            float _Contrast;
            float _Brightness;
            float _Opacity;
            float _JumpStrength;
            float _SecondLayerStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            float randomValue(float seed)
            {
                return frac(sin(seed * 12.9898) * 43758.5453);
            }

            float2 randomOffset(float frame)
            {
                return float2(
                    randomValue(frame + 13.17),
                    randomValue(frame + 91.73)
                );
            }

            v2f vert(appdata_t input)
            {
                v2f output;

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 spriteColor =
                    tex2D(_MainTex, input.uv) * input.color;
// 초당 _FrameRate번만 갱신
float frameRate = max(_FrameRate, 0.001);
float frame = floor(_Time.y * frameRate);

// 계단식으로 끊긴 시간
float steppedTime = frame / frameRate;

float2 jumpOffset =
    randomOffset(frame) * _JumpStrength;

// 매 프레임이 아니라 지정된 횟수만 이동
float2 scrollOffset =
    steppedTime * _ScrollSpeed.xy;

                float2 noiseUV1 =
                    input.uv * _NoiseTiling.xy
                    + scrollOffset
                    + jumpOffset;

                // 두 번째 레이어는 크기와 이동 방향을 다르게 설정
                float2 noiseUV2 =
                    input.uv * (_NoiseTiling.xy * 1.73)
                    - scrollOffset * 0.63
                    + jumpOffset.yx * 1.37;

                float noise1 = tex2D(_NoiseTex, noiseUV1).r;
                float noise2 = tex2D(_NoiseTex, noiseUV2).r;

                float noise = lerp(
                    noise1,
                    noise2,
                    _SecondLayerStrength
                );

                noise =
                    (noise - 0.5) * _Contrast
                    + 0.5
                    + _Brightness;

                noise = saturate(noise);

                fixed4 result;
                result.rgb = noise.xxx * input.color.rgb;
                result.a = spriteColor.a * _Opacity;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(
                    input.worldPosition.xy,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }

            ENDCG
        }
    }
}
