// Unity 6 uGUI moving diagonal stripes with a procedural, bloom-free glow.
// Intended for an overlay Image placed as the last child of a CanvasGroup.
Shader "UI/Moving Glow Stripes"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [HDR] _StripeColor ("Stripe Color", Color) = (1.0, 0.85, 0.2, 0.9)
        _StripeSpacing ("Stripe Spacing", Range(4, 256)) = 72
        _StripeWidth ("Stripe Width", Range(0.01, 0.95)) = 0.34
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.25)) = 0.018
        _GlowWidth ("Glow Width", Range(0, 0.5)) = 0.14
        _GlowOpacity ("Glow Opacity", Range(0, 1)) = 0.45
        _Brightness ("Brightness", Range(0, 8)) = 1.6
        _MoveSpeed ("Move Speed", Float) = 80
        _Direction ("Move Direction (XY)", Vector) = (1, 1, 0, 0)

        [Toggle] _BorderEnabled ("Enable Border", Float) = 1
        [HDR] _BorderColor ("Border Color", Color) = (1.0, 0.85, 0.2, 1.0)
        _RectSize ("Rect Size (Width, Height)", Vector) = (512, 512, 0, 0)
        _RectPivot ("Rect Pivot (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _CornerRadius ("Corner Radius (UI Units)", Range(0, 256)) = 52
        _BorderWidth ("Border Width (UI Units)", Range(0, 32)) = 5
        _BorderGlowWidth ("Border Glow Width (UI Units)", Range(0, 48)) = 7
        _BorderGlowOpacity ("Border Glow Opacity", Range(0, 1)) = 0.4
        _BorderBrightness ("Border Brightness", Range(0, 8)) = 1.8

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
            Name "MovingGlowStripes"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float4 worldPosition : TEXCOORD1;
                float2 localPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _StripeColor;
            float _StripeSpacing;
            float _StripeWidth;
            float _EdgeSoftness;
            float _GlowWidth;
            float _GlowOpacity;
            float _Brightness;
            float _MoveSpeed;
            float4 _Direction;

            float _BorderEnabled;
            fixed4 _BorderColor;
            float4 _RectSize;
            float4 _RectPivot;
            float _CornerRadius;
            float _BorderWidth;
            float _BorderGlowWidth;
            float _BorderGlowOpacity;
            float _BorderBrightness;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.localPosition = input.vertex.xy;
                output.color = input.color * _Color;
                return output;
            }

            // Signed distance to a rounded rectangle in RectTransform local units.
            // Negative values are inside, zero is the edge, positive is outside.
            float RoundedRectDistance(
                float2 localPosition,
                float2 rectSize,
                float2 rectPivot,
                float cornerRadius)
            {
                float2 halfSize = rectSize * 0.5;
                float2 rectCenter = (float2(0.5, 0.5) - rectPivot) * rectSize;
                float2 positionFromCenter = localPosition - rectCenter;
                float2 corner = abs(positionFromCenter) - (halfSize - cornerRadius);
                return length(max(corner, 0.0))
                    + min(max(corner.x, corner.y), 0.0)
                    - cornerRadius;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Only the source sprite's alpha is used. This lets a rounded or
                // sliced sprite act as the exact visible boundary of the effect.
                fixed4 sprite = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;
                float alphaMask = sprite.a * input.color.a;

                float2 direction = _Direction.xy;
                direction *= rsqrt(max(dot(direction, direction), 0.000001));

                float spacing = max(_StripeSpacing, 0.0001);
                float phase = dot(input.localPosition, direction) / spacing;
                phase -= _Time.y * (_MoveSpeed / spacing);

                // Distance from the centre of the nearest repeating stripe.
                float stripeDistance = abs(frac(phase) - 0.5);
                float halfStripeWidth = _StripeWidth * 0.5;

                // fwidth keeps the stripe edge antialiased as the Canvas scales.
                float antialiasing = max(fwidth(phase), 0.0005);
                float edge = max(_EdgeSoftness, antialiasing);
                float core = 1.0 - smoothstep(
                    halfStripeWidth,
                    halfStripeWidth + edge,
                    stripeDistance);

                float glow = 1.0 - smoothstep(
                    halfStripeWidth + edge,
                    halfStripeWidth + edge + max(_GlowWidth, 0.0001),
                    stripeDistance);

                // The halo is drawn directly into the UI, so no Bloom or camera
                // stacking is required. Keep the solid core fully opaque.
                float stripeShape = max(core, glow * _GlowOpacity);
                float stripeAlpha = _StripeColor.a * stripeShape;

                // The border is generated from RectTransform geometry rather than
                // texture UVs, so it stays continuous on Sliced and atlased sprites.
                float2 rectSize = max(_RectSize.xy, float2(0.001, 0.001));
                float maxCornerRadius = min(rectSize.x, rectSize.y) * 0.5;
                float cornerRadius = clamp(_CornerRadius, 0.0, maxCornerRadius);
                float rectDistance = RoundedRectDistance(
                    input.localPosition,
                    rectSize,
                    _RectPivot.xy,
                    cornerRadius);

                float borderWidth = max(_BorderWidth, 0.0);
                float borderGlowWidth = max(_BorderGlowWidth, 0.0);
                float borderAA = max(fwidth(rectDistance), 0.35);

                float insideRect = 1.0 - smoothstep(0.0, borderAA, rectDistance);
                float borderLine = insideRect * smoothstep(
                    -borderWidth - borderAA,
                    -borderWidth + borderAA,
                    rectDistance);
                float borderHalo = insideRect * smoothstep(
                    -(borderWidth + borderGlowWidth) - borderAA,
                    -(borderWidth + borderGlowWidth) + borderAA,
                    rectDistance);

                float borderShape = max(borderLine, borderHalo * _BorderGlowOpacity);
                float borderAlpha = _BorderEnabled * _BorderColor.a * borderShape;

                // Composite the border over the stripes inside this one UI pass.
                float effectAlpha = borderAlpha + stripeAlpha * (1.0 - borderAlpha);
                float3 stripeRGB = _StripeColor.rgb * _Brightness;
                float3 borderRGB = _BorderColor.rgb * _BorderBrightness;
                float3 effectRGB =
                    borderRGB * borderAlpha +
                    stripeRGB * stripeAlpha * (1.0 - borderAlpha);
                effectRGB /= max(effectAlpha, 0.0001);

                float finalAlpha = alphaMask * effectAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                finalAlpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalAlpha - 0.001);
                #endif

                float3 finalColor = effectRGB * input.color.rgb;
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
