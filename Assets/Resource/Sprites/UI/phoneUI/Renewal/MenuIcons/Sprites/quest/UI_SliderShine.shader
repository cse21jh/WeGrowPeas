Shader "UI/Slider Shine"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        /*
         * 머터리얼 자체의 투명도 조절용.
         * RGB는 반짝임 색상에 영향을 주지 않는다.
         */
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Shine)]

        _ShineColor
        (
            "Shine Color",
            Color
        ) = (1.0, 0.95, 0.45, 0.65)

        _ShineIntensity
        (
            "Shine Intensity",
            Range(0, 2)
        ) = 1.0

        _ShineWidth
        (
            "Main Stripe Width",
            Range(0.001, 0.3)
        ) = 0.045

        _EdgeSoftness
        (
            "Edge Softness",
            Range(0.0001, 0.1)
        ) = 0.008

        _Slant
        (
            "Stripe Slant",
            Range(-1, 1)
        ) = 0.06

        [Header(Animation)]

        _MoveDuration
        (
            "Move Duration",
            Range(0.05, 3)
        ) = 0.75

        _Interval
        (
            "Repeat Interval",
            Range(0, 5)
        ) = 0.75

        _StartOffset
        (
            "Start Time Offset",
            Float
        ) = 0

        [Header(Secondary Stripe)]

        _SecondStripeStrength
        (
            "Second Stripe Strength",
            Range(0, 1)
        ) = 0.55

        _SecondStripeGap
        (
            "Second Stripe Gap",
            Range(0, 0.3)
        ) = 0.08

        _SecondStripeWidth
        (
            "Second Stripe Width",
            Range(0.001, 0.2)
        ) = 0.018

        /*
         * UGUI Mask용 스텐실 프로퍼티
         */
        [HideInInspector]
        _StencilComp ("Stencil Comparison", Float) = 8

        [HideInInspector]
        _Stencil ("Stencil ID", Float) = 0

        [HideInInspector]
        _StencilOp ("Stencil Operation", Float) = 0

        [HideInInspector]
        _StencilWriteMask ("Stencil Write Mask", Float) = 255

        [HideInInspector]
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector]
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

        /*
         * 일반 투명 합성.
         *
         * 반짝임 알파가 0인 부분에서는
         * 부모의 원래 색상이 그대로 유지된다.
         */
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                float4 worldPosition : TEXCOORD0;
                float2 texcoord      : TEXCOORD1;
                fixed4 color         : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _ShineColor;

            float4 _ClipRect;

            float _ShineIntensity;
            float _ShineWidth;
            float _EdgeSoftness;
            float _Slant;

            float _MoveDuration;
            float _Interval;
            float _StartOffset;

            float _SecondStripeStrength;
            float _SecondStripeGap;
            float _SecondStripeWidth;

            v2f vert(appdata_t input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);

                output.texcoord = input.texcoord;
                output.color = input.color;

                return output;
            }

            /*
             * 중심 위치와 너비를 기준으로
             * 부드러운 사각형 띠를 생성한다.
             */
            float CreateStripe(
                float position,
                float center,
                float width,
                float softness
            )
            {
                float halfWidth = max(width * 0.5, 0.0001);
                float safeSoftness = max(softness, 0.0001);

                float distanceFromCenter = abs(
                    position - center
                );

                return 1.0 - smoothstep(
                    halfWidth,
                    halfWidth + safeSoftness,
                    distanceFromCenter
                );
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float moveDuration = max(
                    _MoveDuration,
                    0.001
                );

                float cycleDuration = max(
                    _MoveDuration + _Interval,
                    0.001
                );

                /*
                 * 현재 반복 주기 안에서의 시간
                 */
                float cycleTime = fmod(
                    _Time.y + _StartOffset,
                    cycleDuration
                );

                /*
                 * 이동 시간이 끝난 뒤에는
                 * 반짝임을 완전히 숨긴다.
                 */
                float isMoving =
                    1.0 - step(moveDuration, cycleTime);

                float moveProgress = saturate(
                    cycleTime / moveDuration
                );

                /*
                 * Y 좌표에 따라 X 좌표를 보정하여
                 * 세로 사각형을 사선으로 기울인다.
                 */
                float slantedPosition =
                    input.texcoord.x
                    - (
                        (input.texcoord.y - 0.5)
                        * _Slant
                    );

                /*
                 * 반짝임이 화면 바깥에서 시작하고 끝나도록
                 * 이동 여백을 계산한다.
                 */
                float travelMargin =
                    abs(_Slant) * 0.5
                    + _ShineWidth
                    + _SecondStripeGap
                    + _SecondStripeWidth
                    + 0.03;

                float stripeCenter = lerp(
                    -travelMargin,
                    1.0 + travelMargin,
                    moveProgress
                );

                /*
                 * 메인 반짝임 띠
                 */
                float mainStripe = CreateStripe(
                    slantedPosition,
                    stripeCenter,
                    _ShineWidth,
                    _EdgeSoftness
                );

                /*
                 * 메인 띠 뒤를 따라오는 보조 띠
                 */
                float secondStripe = CreateStripe(
                    slantedPosition,
                    stripeCenter - _SecondStripeGap,
                    _SecondStripeWidth,
                    _EdgeSoftness
                );

                secondStripe *= _SecondStripeStrength;

                float shine = max(
                    mainStripe,
                    secondStripe
                );

                shine *= isMoving;

                /*
                 * 반짝이는 띠 부분에만 알파가 생긴다.
                 *
                 * 띠가 없는 영역은 알파가 0이므로
                 * 부모 Fill의 원본 색상에 아무 영향도 주지 않는다.
                 */
                float resultAlpha =
                    shine
                    * _ShineColor.a
                    * _ShineIntensity
                    * input.color.a
                    * _Color.a;

                resultAlpha = saturate(resultAlpha);

                /*
                 * RectMask2D 대응
                 */
                #ifdef UNITY_UI_CLIP_RECT
                resultAlpha *= UnityGet2DClipping(
                    input.worldPosition.xy,
                    _ClipRect
                );
                #endif

                /*
                 * UI 알파 클리핑 대응
                 */
                #ifdef UNITY_UI_ALPHACLIP
                clip(resultAlpha - 0.001);
                #endif

                /*
                 * Image의 주황색 RGB는 사용하지 않는다.
                 * 지정한 Shine Color만 출력한다.
                 */
                return fixed4(
                    _ShineColor.rgb,
                    resultAlpha
                );
            }

            ENDCG
        }
    }
}
