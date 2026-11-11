// Assign to a uGUI Image. The original sprite RGB and alpha are preserved
// outside the moving reflection. For screen-only shine, use on the separate
// screen Image and leave the frame Image on its normal material.
Shader "UI/Phone Glass Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Glass Reflection)]
        _ShineColor ("Reflection Color (Alpha = Strength)", Color) = (0.78, 0.88, 0.96, 0.24)
        _Angle ("Stripe Angle (Degrees)", Range(-180, 180)) = 45
        _Spacing ("Repeat Spacing (UI Units)", Float) = 80
        _Width ("Wide Stripe Width (UI Units)", Float) = 12
        _Softness ("Edge Softness (UI Units)", Float) = 1.4
        _ThinWidth ("Thin Stripe Width (UI Units)", Float) = 3
        _ThinOffset ("Thin Stripe Offset (UI Units)", Float) = 13
        _ThinStrength ("Thin Stripe Strength", Range(0, 1)) = 0.7

        [Header(Motion)]
        _Speed ("Travel Speed (UI Units Per Second)", Float) = 8
        _Phase ("Start Phase", Range(0, 1)) = 0.2
        [HideInInspector] _UseCurve ("Use Scripted Curve", Float) = 0
        [HideInInspector] _CurvePhase ("Curve Progress", Float) = 0

        // Standard uGUI stencil and clipping properties are required for Mask,
        // RectMask2D, CanvasGroup fades and UI materialForRendering variants.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "PhoneGlassShine"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                half4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            fixed4 _ShineColor;
            float _Angle, _Spacing, _Width, _Softness;
            float _ThinWidth, _ThinOffset, _ThinStrength;
            float _Speed, _Phase;
            float _UseCurve, _CurvePhase;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.localPosition = input.vertex.xy;
                output.color = input.color * _Color;

                float2 pixelSize = output.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = half4(input.vertex.xy * 2 - rect.xy - rect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                return output;
            }

            float Stripe(float position, float center, float width, float spacing, float edge)
            {
                // Circular distance avoids a visible jump at the repeat seam.
                float distance = abs(frac((position - center) / spacing + 0.5) - 0.5) * spacing;
                float halfWidth = min(max(width, 0), spacing * 0.95) * 0.5;
                return (1 - smoothstep(halfWidth, halfWidth + edge, distance)) * step(0.0001, width);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float angle = radians(_Angle);
                float2 normal = float2(sin(angle), -cos(angle));

                // Object-space positions keep a physical 45-degree reflection
                // on tall images and continuous stripes on sliced/atlased sprites.
                float position = dot(input.localPosition, normal);
                float spacing = max(abs(_Spacing), 0.001);
                // A per-Image material modifier can supply an AnimationCurve.
                // Without that component, the material keeps its constant speed.
                float travel = lerp(_Time.y * _Speed / spacing, _CurvePhase, _UseCurve);
                float center = frac(travel + _Phase) * spacing;
                float edge = max(max(_Softness, fwidth(position)), 0.001);
                float wide = Stripe(position, center, _Width, spacing, edge);
                float thin = Stripe(position, center - _ThinOffset, _ThinWidth, spacing, edge);
                float reflection = saturate(max(wide, thin * _ThinStrength) * _ShineColor.a);

                // Alpha is never increased: no rectangular overlay appears in
                // the sprite's transparent padding or around the phone silhouette.
                color.rgb = lerp(color.rgb, _ShineColor.rgb * input.color.rgb, reflection);

                #ifdef UNITY_UI_CLIP_RECT
                half2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                color.a *= mask.x * mask.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
