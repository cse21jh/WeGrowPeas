Shader "WeGrowPeas/UI/Figma Alpha Blend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BackgroundLinear ("Backing color in linear space", Vector) = (0,0,0,0)
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 position:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Color, _TextureSampleAdd, _ClipRect, _BackgroundLinear;
            v2f vert(appdata v)
            {
                v2f o; o.position=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.position.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-0.001);
                #endif
                // Figma composites translucent fills in sRGB. Match that appearance in a
                // Linear Unity project over the known, uniform backing of this UI slot.
                #ifndef UNITY_COLORSPACE_GAMMA
                float3 b=_BackgroundLinear.rgb;
                float3 target=GammaToLinearSpace(lerp(LinearToGammaSpace(b),LinearToGammaSpace(c.rgb),c.a));
                float3 needed=1-target/max(b,0.00001);
                float a=max(c.a,max(needed.r,max(needed.g,needed.b)));
                c.rgb=saturate((target-b*(1-a))/max(a,0.00001));
                c.a=saturate(a);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
