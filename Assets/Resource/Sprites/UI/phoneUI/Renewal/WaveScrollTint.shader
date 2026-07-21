Shader "UI/WaveScrollMaskable"
{
    Properties
    {
        [PerRendererData] _MainTex ("Wave Texture", 2D) = "white" {}

        _WaveColor ("Wave Color", Color) = (1, 1, 1, 1)
        _ScrollSpeed ("Scroll Speed XY", Vector) = (0.01, 0, 0, 0)
        _Tiling ("Tiling XY", Vector) = (1, 1, 0, 0)
        _Offset ("Offset XY", Vector) = (0, 0, 0, 0)

        [HideInInspector] _StencilComp
            ("Stencil Comparison", Float) = 8

        [HideInInspector] _Stencil
            ("Stencil ID", Float) = 0

        [HideInInspector] _StencilOp
            ("Stencil Operation", Float) = 0

        [HideInInspector] _StencilWriteMask
            ("Stencil Write Mask", Float) = 255

        [HideInInspector] _StencilReadMask
            ("Stencil Read Mask", Float) = 255

        [HideInInspector] _ColorMask
            ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip
            ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
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

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 localPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float4 _WaveColor;
            float4 _ScrollSpeed;
            float4 _Tiling;
            float4 _Offset;
            float4 _ClipRect;

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.localPosition = input.positionOS;
                output.positionCS =
                    UnityObjectToClipPos(input.positionOS);

                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv;

                // X축만 무한 반복
                uv.x = frac(
                    input.uv.x * _Tiling.x
                    + _Offset.x
                    + _ScrollSpeed.x * _Time.y
                );

                // Y축은 절대로 반복하지 않음
                float rawY =
                    input.uv.y * _Tiling.y
                    + _Offset.y;

                // 텍스처 끝 픽셀이 반대쪽 픽셀과 섞이지 않도록
                // 반 픽셀 안쪽으로 Clamp
                float halfTexelY = _MainTex_TexelSize.y * 0.5;

                uv.y = clamp(
                    rawY,
                    halfTexelY,
                    1.0 - halfTexelY
                );

                // 원본 PNG에 들어 있는 알파 채널을 직접 사용한다.
                half textureAlpha = tex2D(_MainTex, uv).a;

                half4 color;
                color.rgb =
                    _WaveColor.rgb * input.color.rgb;

                color.a =
                    textureAlpha
                    * _WaveColor.a
                    * input.color.a;

                // RectMask2D 지원
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(
                    input.localPosition.xy,
                    _ClipRect
                );
                #endif

                // 일반 UGUI Mask 지원
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001h);
                #endif

                return color;
            }

            ENDHLSL
        }
    }
}
