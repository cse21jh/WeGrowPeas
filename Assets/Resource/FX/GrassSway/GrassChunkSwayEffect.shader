Shader "Custom/2D/GrassChunkSwayEffect"
{
    Properties
    {
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}

        _SwayPower("SwayPower", Range(0, 5)) = 0.2
        _WindSpeed("WindSpeed", Range(0, 5)) = 1

        _DissolveAmount("DissolveAmount", Range(0, 1)) = 0
        _OutlineThickness("OutlineThickness", Range(0, 1)) = 0.1
        _DissolveScale("DissolveScale", Range(0, 500)) = 30

        _Dryness("Dryness", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GrassChunkUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _SwayPower;
                float _WindSpeed;
                float _DissolveAmount;
                float _OutlineThickness;
                float _DissolveScale;
                float _Dryness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;

                // x = random phase
                // y = random strength
                // z = local height, bottom 0 ~ top 1
                // w = local x, left 0 ~ right 1
                float4 custom : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 GradientDirection(float2 p)
            {
                float angle = Hash21(p) * 6.2831853;
                return float2(cos(angle), sin(angle));
            }

            float GradientNoise(float2 uv, float scale)
            {
                scale = max(scale, 0.001);

                float2 p = uv * scale;
                float2 ip = floor(p);
                float2 fp = frac(p);

                float d00 = dot(GradientDirection(ip), fp);
                float d01 = dot(GradientDirection(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(GradientDirection(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(GradientDirection(ip + float2(1, 1)), fp - float2(1, 1));

                fp = fp * fp * fp * (fp * (fp * 6.0 - 15.0) + 10.0);

                return lerp(
                    lerp(d00, d01, fp.y),
                    lerp(d10, d11, fp.y),
                    fp.x
                ) + 0.5;
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                float bottom = lerp(a, b, f.x);
                float top = lerp(c, d, f.x);

                return lerp(bottom, top, f.y);
            }

            float SimpleNoise(float2 uv, float scale)
            {
                scale = max(scale, 0.001);

                float result = 0.0;

                // Shader Graph Simple Noise와 비슷하게 3 octave 구성
                result += ValueNoise(uv * scale) * 0.125;
                result += ValueNoise(uv * scale * 0.5) * 0.25;
                result += ValueNoise(uv * scale * 0.25) * 0.5;

                return saturate(result);
            }

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 HueShift(float3 color, float degrees)
            {
                float3 hsv = RgbToHsv(color);
                hsv.x = frac(hsv.x + degrees / 360.0);
                return HsvToRgb(hsv);
            }

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.2126729, 0.7151522, 0.0721750));
                return luma.xxx + saturation * (color - luma.xxx);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = input.positionOS;
                float3 positionWS = TransformObjectToWorld(positionOS);

                float phase = input.custom.x;
                float strength = input.custom.y;
                float localHeight = saturate(input.custom.z);

                float heightMask = localHeight * localHeight;

                // 큰 바람: 월드 좌표 기반.
                // 전체 풀이 하나의 바람장을 공유한다.
                float2 windUV = positionWS.xy + (_Time.y * _WindSpeed).xx;
                float windNoise = GradientNoise(windUV, _SwayPower);
                float globalWind = windNoise - 0.5;

                // 작은 떨림: 풀마다 약간의 차이만 준다.
                // 비중을 아주 낮게 둔다.
                float localFlutter = sin(_Time.y * _WindSpeed * 1.7 + phase) * 0.05;

                // 최종 흔들림
                float sway = (globalWind + localFlutter) * heightMask * strength;

                positionOS.x += sway;

                output.positionHCS = TransformObjectToHClip(positionOS);
                output.uv0 = input.uv0;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv0);

                float dissolveNoise = SimpleNoise(input.uv0, _DissolveScale);

                float dissolveThreshold = 1.0 - _DissolveAmount;
                float outlineThreshold = dissolveThreshold + _OutlineThickness;

                float visibleMask = step(dissolveNoise, outlineThreshold);
                float innerMask = step(dissolveNoise, dissolveThreshold);
                float outlineMask = saturate(visibleMask - innerMask);

                float3 outlineColor = float3(0.1924528, 0.1141578, 0.09114272);

                col.rgb = lerp(col.rgb, outlineColor, outlineMask);

                float3 dryColor = HueShift(col.rgb, 323.7);
                dryColor = ApplySaturation(dryColor, 0.7);

                col.rgb = lerp(col.rgb, dryColor, _Dryness);
                col.a *= visibleMask;

                return col;
            }

            ENDHLSL
        }
    }
}
