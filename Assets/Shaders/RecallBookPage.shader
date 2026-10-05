Shader "WeGrowPeas/UI/Toon Book Page"
{
    Properties
    {
        [PerRendererData] _MainTex ("Front spread", 2D) = "white" {}
        _BackTex ("Back spread", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowColor ("Warm shadow tint", Color) = (0.74,0.56,0.41,1)
        _ShadowStrength ("Shadow strength", Range(0,1)) = 0.24
        _Bands ("Cel bands", Range(2,5)) = 3
        _OutlineColor ("Paper edge", Color) = (0.55,0.30,0.16,1)
        _OutlineWidth ("Paper edge width (UV)", Range(0,0.02)) = 0.0025
        _FrontRect ("Front UV rect", Vector) = (0.5,0,0.5,1)
        _BackRect ("Back UV rect", Vector) = (0,0,0.5,1)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 sheet:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 sheet:TEXCOORD1; float4 local:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex, _BackTex;
            fixed4 _Color, _ShadowColor, _OutlineColor;
            float4 _FrontRect, _BackRect, _ClipRect;
            float _ShadowStrength, _Bands, _OutlineWidth;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                o.sheet = v.sheet;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv = saturate(i.uv);
                fixed4 front = tex2D(_MainTex, _FrontRect.xy + uv * _FrontRect.zw);
                fixed4 back = tex2D(_BackTex, _BackRect.xy + uv * _BackRect.zw);
                fixed4 col = lerp(front, back, step(0.5, i.sheet.x)) * i.color;
                float bands = max(2, floor(_Bands + 0.5));
                float shade = floor(saturate(i.sheet.y) * (bands - 1) + 0.5) / (bands - 1);
                col.rgb *= lerp(1, _ShadowColor.rgb, (1 - shade) * _ShadowStrength);
                float edge = min(min(uv.x, 1 - uv.x), min(uv.y, 1 - uv.y));
                float aa = max(fwidth(edge), 0.0001);
                float outline = _OutlineWidth > 0 ? 1 - smoothstep(_OutlineWidth - aa, _OutlineWidth + aa, edge) : 0;
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
