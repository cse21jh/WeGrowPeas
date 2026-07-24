Shader "Custom/TileHighlight"
{
    Properties
    {
        [HDR] _StripeColor ("Stripe Color", Color) = (1.0, 1.0, 0.2, 1.0)
        _StripeDensity ("Stripe Density", Float) = 10.0
        _StripeSpeed ("Stripe Speed", Float) = 2.0
        _StripeWidth ("Stripe Width", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _StripeColor;
            float _StripeDensity;
            float _StripeSpeed;
            float _StripeWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 안쪽 빗금(Stripes) 판별
                float diagonal = i.uv.x + i.uv.y;
                diagonal += _Time.y * _StripeSpeed;
                
                float stripePattern = frac(diagonal * _StripeDensity);
                float isStripe = step(stripePattern, _StripeWidth);

                if (isStripe > 0.5)
                {
                    return _StripeColor;
                }
                
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
