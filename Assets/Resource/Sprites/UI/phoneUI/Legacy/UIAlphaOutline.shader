Shader "UI/Alpha Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [HDR] _OutlineColor ("Outline Color", Color) = (0.2, 0.25, 0.08, 1)
        [Enum(Outside, 0, Center, 1, Inside, 2)]
        _OutlineMode ("Outline Position", Float) = 0
        _OutlineWidth ("Outline Width", Range(0, 32)) = 4
        _OutlineSoftness ("Outline Softness", Range(0, 1)) = 1
        [Enum(Screen Pixels, 0, Texture Pixels, 1)]
        _WidthMode ("Width Unit", Float) = 0

        // Required by uGUI masking and CanvasRenderer.
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
            Name "UIAlphaOutline"

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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            fixed4 _Color;
            fixed4 _OutlineColor;
            fixed4 _TextureSampleAdd;

            float4 _ClipRect;
            float _OutlineMode;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _WidthMode;

            v2f vert(appdata_t input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;

                return output;
            }

            inline half SampleSpriteAlpha(float2 uv)
            {
                return saturate((tex2D(_MainTex, uv) + _TextureSampleAdd).a);
            }

            inline void AccumulateAlpha(
                float2 uv,
                float2 offset,
                inout half minimumAlpha,
                inout half maximumAlpha)
            {
                half sampledAlpha = SampleSpriteAlpha(uv + offset);
                minimumAlpha = min(minimumAlpha, sampledAlpha);
                maximumAlpha = max(maximumAlpha, sampledAlpha);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 baseColor =
                    (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd)
                    * input.color;

                half centerAlpha = saturate(baseColor.a);

                // Center mode places half of the requested width on each side.
                float radius = _OutlineWidth;
                if (_OutlineMode > 0.5 && _OutlineMode < 1.5)
                {
                    radius *= 0.5;
                }

                // Screen Pixels keeps the visible width comparatively stable
                // when the RectTransform or Canvas scale changes.
                float2 screenPixelUV = max(
                    fwidth(input.texcoord),
                    float2(0.000001, 0.000001));

                float useTexturePixels = step(0.5, _WidthMode);
                float2 uvPerPixel = lerp(
                    screenPixelUV,
                    _MainTex_TexelSize.xy,
                    useTexturePixels);

                float2 radiusUV = uvPerPixel * radius;

                half minimumAlpha = centerAlpha;
                half maximumAlpha = centerAlpha;

                // Twelve normalized directions. This is smoother than eight
                // taps while remaining suitable for ordinary mobile UI use.
                AccumulateAlpha(input.texcoord, radiusUV * float2( 1.000000,  0.000000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.866025,  0.500000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.500000,  0.866025), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.000000,  1.000000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2(-0.500000,  0.866025), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2(-0.866025,  0.500000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2(-1.000000,  0.000000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2(-0.866025, -0.500000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2(-0.500000, -0.866025), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.000000, -1.000000), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.500000, -0.866025), minimumAlpha, maximumAlpha);
                AccumulateAlpha(input.texcoord, radiusUV * float2( 0.866025, -0.500000), minimumAlpha, maximumAlpha);

                half outsideMask = saturate(maximumAlpha - centerAlpha);
                half insideMask = saturate(centerAlpha - minimumAlpha);

                half rawOutlineMask = outsideMask;

                if (_OutlineMode > 1.5)
                {
                    rawOutlineMask = insideMask;
                }
                else if (_OutlineMode > 0.5)
                {
                    rawOutlineMask = max(outsideMask, insideMask);
                }

                // 0 = hard edge, 1 = preserve the texture's antialiased edge.
                half hardMask = step(0.0001h, rawOutlineMask);
                half outlineMask = lerp(
                    hardMask,
                    rawOutlineMask,
                    saturate(_OutlineSoftness));

                // Draw the outline over the source using straight-alpha
                // source-over composition.
                half outlineAlpha =
                    outlineMask
                    * _OutlineColor.a
                    * input.color.a;

                half outputAlpha =
                    outlineAlpha
                    + baseColor.a * (1.0h - outlineAlpha);

                half3 premultipliedRGB =
                    _OutlineColor.rgb * outlineAlpha
                    + baseColor.rgb * baseColor.a * (1.0h - outlineAlpha);

                fixed4 outputColor;
                outputColor.rgb =
                    premultipliedRGB / max(outputAlpha, 0.0001h);
                outputColor.a = outputAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                outputColor.a *= UnityGet2DClipping(
                    input.worldPosition.xy,
                    _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outputColor.a - 0.001h);
                #endif

                return outputColor;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
