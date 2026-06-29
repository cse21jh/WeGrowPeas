Shader "Custom/2D/GrassChunkSwayEffect"
{

    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        _SwayPower("Sway Power", Range(0, 5)) = 0.25
        _WindSpeed("Wind Speed", Range(0, 5)) = 0.8
        _SwayAmplitude("Sway Amplitude", Range(0, 1)) = 0.25
        _LocalFlutter("Local Flutter", Range(0, 0.3)) = 0.03

        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _OutlineThickness("Outline Thickness", Range(0, 1)) = 0.1
        _DissolveScale("Dissolve Scale", Range(0, 500)) = 30
        _OutlineColor("Outline Color", Color) = (0.1924528, 0.1141578, 0.09114272, 1)
        _Dryness("Dryness", Range(0, 1)) = 0
        [HideInInspector] _White("Tint", Color) = (1,1,1,1)  // Added to break SRP batching. Work around for issue with SRP Batching
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual

        Stencil
        {
            Ref 128 // Put this in the last bit of our stencil value for maximum compatibility with sprite mask
            Comp always
            Pass replace
        }

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                COMMON_2D_INPUTS

                // GrassChunkController에서 SetUVs(1, customData)로 넘기는 값
                // x = phase
                // y = strength
                // z = local height, bottom 0 ~ top 1
                // w = local x
                float4 custom : TEXCOORD1;
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                float2 grassUV : TEXCOORD7;
            };

            float4 _White;

            float _SwayPower;
            float _WindSpeed;
            float _SwayAmplitude;
            float _LocalFlutter;
            float _DissolveAmount;
            float _OutlineThickness;
            float _DissolveScale;
            float4 _OutlineColor;
            float _Dryness;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 GradientDir(float2 p)
            {
                float angle = Hash21(p) * 6.2831853;
                return float2(cos(angle), sin(angle));
            }

            float GradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);

                float2 smoothF = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float d00 = dot(GradientDir(i + float2(0, 0)), f - float2(0, 0));
                float d10 = dot(GradientDir(i + float2(1, 0)), f - float2(1, 0));
                float d01 = dot(GradientDir(i + float2(0, 1)), f - float2(0, 1));
                float d11 = dot(GradientDir(i + float2(1, 1)), f - float2(1, 1));

                float x0 = lerp(d00, d10, smoothF.x);
                float x1 = lerp(d01, d11, smoothF.x);

                return lerp(x0, x1, smoothF.y) + 0.5;
            }

            float3 ApplyGrassSway(float3 positionOS, float4 custom)
            {
                float phase = custom.x;
                float strength = custom.y;
                float localHeight = saturate(custom.z);

                float heightMask = localHeight * localHeight;

                // 월드 좌표 기반의 공통 바람장.
                // 가까운 풀들이 같은 바람 값을 공유해서 전체가 함께 흔들리게 만든다.
                float3 positionWS = TransformObjectToWorld(positionOS);
                float2 windUV = positionWS.xy + (_Time.y * _WindSpeed).xx;

                float windNoise = GradientNoise(windUV, _SwayPower);
                float globalWind = windNoise - 0.5;

                // 개별 풀마다 아주 약한 보조 흔들림만 부여한다.
                float localFlutter = sin(_Time.y * _WindSpeed * 1.7 + phase) * _LocalFlutter;

                float sway = (globalWind + localFlutter)
                           * _SwayAmplitude
                           * heightMask
                           * strength;

                positionOS.x += sway;

                return positionOS;
            }




            float GrassValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float GrassSimpleNoise(float2 uv, float scale)
            {
                float noise = 0.0;
                noise += GrassValueNoise(uv * scale) * 0.5;
                noise += GrassValueNoise(uv * scale * 0.5) * 0.25;
                noise += GrassValueNoise(uv * scale * 0.25) * 0.125;
                return noise;
            }

            float3 GrassRGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            float3 GrassHSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 GrassHueShiftDegrees(float3 color, float offsetDegrees)
            {
                float3 hsv = GrassRGBToHSV(color);
                hsv.x = frac(hsv.x + offsetDegrees / 360.0);
                return GrassHSVToRGB(hsv);
            }

            float3 GrassApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.2126729, 0.7151522, 0.0721750));
                return luma.xxx + saturation * (color - luma.xxx);
            }

            void GetGrassDissolveMasks(float2 uv, out float visibleMask, out float outlineMask)
            {
                float dissolveNoise = GrassSimpleNoise(uv, _DissolveScale);

                float dissolveThreshold = 1.0 - _DissolveAmount;
                float outlineThreshold = dissolveThreshold + _OutlineThickness;

                visibleMask = step(dissolveNoise, outlineThreshold);
                float innerMask = step(dissolveNoise, dissolveThreshold);
                outlineMask = saturate(visibleMask - innerMask);
            }

            half4 ApplyGrassSurfaceEffects(half4 litColor, float2 uv)
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float visibleMask;
                float outlineMask;
                GetGrassDissolveMasks(uv, visibleMask, outlineMask);

                // Do not draw dissolve/outline in fully transparent parts of the atlas cell.
                float alphaPresence = step(0.001, baseSample.a);
                visibleMask *= alphaPresence;
                outlineMask *= alphaPresence;

                litColor.rgb = lerp(litColor.rgb, _OutlineColor.rgb, outlineMask);

                float3 dryColor = GrassHueShiftDegrees(litColor.rgb, 323.7);
                dryColor = GrassApplySaturation(dryColor, 0.7);
                litColor.rgb = lerp(litColor.rgb, dryColor, _Dryness);

                litColor.a *= visibleMask;
                return litColor;
            }


            Varyings LitVertex(Attributes input)
            {
                input.positionOS = ApplyGrassSway(input.positionOS, input.custom);
                Varyings output = CommonLitVertex(input);
                output.grassUV = input.uv;
                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 litColor = CommonLitFragment(input, _White);
                return ApplyGrassSurfaceEffects(litColor, input.grassUV);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS

                // GrassChunkController에서 SetUVs(1, customData)로 넘기는 값
                // x = phase
                // y = strength
                // z = local height, bottom 0 ~ top 1
                // w = local x
                float4 custom : TEXCOORD1;
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                float2 grassUV : TEXCOORD7;
            };

            float4 _White;

            float _SwayPower;
            float _WindSpeed;
            float _SwayAmplitude;
            float _LocalFlutter;
            float _DissolveAmount;
            float _OutlineThickness;
            float _DissolveScale;
            float4 _OutlineColor;
            float _Dryness;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 GradientDir(float2 p)
            {
                float angle = Hash21(p) * 6.2831853;
                return float2(cos(angle), sin(angle));
            }

            float GradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);

                float2 smoothF = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float d00 = dot(GradientDir(i + float2(0, 0)), f - float2(0, 0));
                float d10 = dot(GradientDir(i + float2(1, 0)), f - float2(1, 0));
                float d01 = dot(GradientDir(i + float2(0, 1)), f - float2(0, 1));
                float d11 = dot(GradientDir(i + float2(1, 1)), f - float2(1, 1));

                float x0 = lerp(d00, d10, smoothF.x);
                float x1 = lerp(d01, d11, smoothF.x);

                return lerp(x0, x1, smoothF.y) + 0.5;
            }

            float3 ApplyGrassSway(float3 positionOS, float4 custom)
            {
                float phase = custom.x;
                float strength = custom.y;
                float localHeight = saturate(custom.z);

                float heightMask = localHeight * localHeight;

                // 월드 좌표 기반의 공통 바람장.
                // 가까운 풀들이 같은 바람 값을 공유해서 전체가 함께 흔들리게 만든다.
                float3 positionWS = TransformObjectToWorld(positionOS);
                float2 windUV = positionWS.xy + (_Time.y * _WindSpeed).xx;

                float windNoise = GradientNoise(windUV, _SwayPower);
                float globalWind = windNoise - 0.5;

                // 개별 풀마다 아주 약한 보조 흔들림만 부여한다.
                float localFlutter = sin(_Time.y * _WindSpeed * 1.7 + phase) * _LocalFlutter;

                float sway = (globalWind + localFlutter)
                           * _SwayAmplitude
                           * heightMask
                           * strength;

                positionOS.x += sway;

                return positionOS;
            }




            float GrassValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float GrassSimpleNoise(float2 uv, float scale)
            {
                float noise = 0.0;
                noise += GrassValueNoise(uv * scale) * 0.5;
                noise += GrassValueNoise(uv * scale * 0.5) * 0.25;
                noise += GrassValueNoise(uv * scale * 0.25) * 0.125;
                return noise;
            }

            float3 GrassRGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            float3 GrassHSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 GrassHueShiftDegrees(float3 color, float offsetDegrees)
            {
                float3 hsv = GrassRGBToHSV(color);
                hsv.x = frac(hsv.x + offsetDegrees / 360.0);
                return GrassHSVToRGB(hsv);
            }

            float3 GrassApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.2126729, 0.7151522, 0.0721750));
                return luma.xxx + saturation * (color - luma.xxx);
            }

            void GetGrassDissolveMasks(float2 uv, out float visibleMask, out float outlineMask)
            {
                float dissolveNoise = GrassSimpleNoise(uv, _DissolveScale);

                float dissolveThreshold = 1.0 - _DissolveAmount;
                float outlineThreshold = dissolveThreshold + _OutlineThickness;

                visibleMask = step(dissolveNoise, outlineThreshold);
                float innerMask = step(dissolveNoise, dissolveThreshold);
                outlineMask = saturate(visibleMask - innerMask);
            }

            half4 ApplyGrassSurfaceEffects(half4 litColor, float2 uv)
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float visibleMask;
                float outlineMask;
                GetGrassDissolveMasks(uv, visibleMask, outlineMask);

                // Do not draw dissolve/outline in fully transparent parts of the atlas cell.
                float alphaPresence = step(0.001, baseSample.a);
                visibleMask *= alphaPresence;
                outlineMask *= alphaPresence;

                litColor.rgb = lerp(litColor.rgb, _OutlineColor.rgb, outlineMask);

                float3 dryColor = GrassHueShiftDegrees(litColor.rgb, 323.7);
                dryColor = GrassApplySaturation(dryColor, 0.7);
                litColor.rgb = lerp(litColor.rgb, dryColor, _Dryness);

                litColor.a *= visibleMask;
                return litColor;
            }


            Varyings NormalsRenderingVertex(Attributes input)
            {
                input.positionOS = ApplyGrassSway(input.positionOS, input.custom);
                Varyings output = CommonNormalsVertex(input);
                output.grassUV = input.uv;
                return output;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                half4 normalColor = CommonNormalsFragment(input, _White);

                float visibleMask;
                float outlineMask;
                GetGrassDissolveMasks(input.grassUV, visibleMask, outlineMask);

                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.grassUV);
                visibleMask *= step(0.001, baseSample.a);

                normalColor.a *= visibleMask;
                return normalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            // GPU Instancing
            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS

                // GrassChunkController에서 SetUVs(1, customData)로 넘기는 값
                // x = phase
                // y = strength
                // z = local height, bottom 0 ~ top 1
                // w = local x
                float4 custom : TEXCOORD1;
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                float2 grassUV : TEXCOORD7;
            };

            float4 _White;

            float _SwayPower;
            float _WindSpeed;
            float _SwayAmplitude;
            float _LocalFlutter;
            float _DissolveAmount;
            float _OutlineThickness;
            float _DissolveScale;
            float4 _OutlineColor;
            float _Dryness;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 GradientDir(float2 p)
            {
                float angle = Hash21(p) * 6.2831853;
                return float2(cos(angle), sin(angle));
            }

            float GradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);

                float2 smoothF = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float d00 = dot(GradientDir(i + float2(0, 0)), f - float2(0, 0));
                float d10 = dot(GradientDir(i + float2(1, 0)), f - float2(1, 0));
                float d01 = dot(GradientDir(i + float2(0, 1)), f - float2(0, 1));
                float d11 = dot(GradientDir(i + float2(1, 1)), f - float2(1, 1));

                float x0 = lerp(d00, d10, smoothF.x);
                float x1 = lerp(d01, d11, smoothF.x);

                return lerp(x0, x1, smoothF.y) + 0.5;
            }

            float3 ApplyGrassSway(float3 positionOS, float4 custom)
            {
                float phase = custom.x;
                float strength = custom.y;
                float localHeight = saturate(custom.z);

                float heightMask = localHeight * localHeight;

                // 월드 좌표 기반의 공통 바람장.
                // 가까운 풀들이 같은 바람 값을 공유해서 전체가 함께 흔들리게 만든다.
                float3 positionWS = TransformObjectToWorld(positionOS);
                float2 windUV = positionWS.xy + (_Time.y * _WindSpeed).xx;

                float windNoise = GradientNoise(windUV, _SwayPower);
                float globalWind = windNoise - 0.5;

                // 개별 풀마다 아주 약한 보조 흔들림만 부여한다.
                float localFlutter = sin(_Time.y * _WindSpeed * 1.7 + phase) * _LocalFlutter;

                float sway = (globalWind + localFlutter)
                           * _SwayAmplitude
                           * heightMask
                           * strength;

                positionOS.x += sway;

                return positionOS;
            }




            float GrassValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float GrassSimpleNoise(float2 uv, float scale)
            {
                float noise = 0.0;
                noise += GrassValueNoise(uv * scale) * 0.5;
                noise += GrassValueNoise(uv * scale * 0.5) * 0.25;
                noise += GrassValueNoise(uv * scale * 0.25) * 0.125;
                return noise;
            }

            float3 GrassRGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;

                return float3(
                    abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                    d / (q.x + e),
                    q.x
                );
            }

            float3 GrassHSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 GrassHueShiftDegrees(float3 color, float offsetDegrees)
            {
                float3 hsv = GrassRGBToHSV(color);
                hsv.x = frac(hsv.x + offsetDegrees / 360.0);
                return GrassHSVToRGB(hsv);
            }

            float3 GrassApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.2126729, 0.7151522, 0.0721750));
                return luma.xxx + saturation * (color - luma.xxx);
            }

            void GetGrassDissolveMasks(float2 uv, out float visibleMask, out float outlineMask)
            {
                float dissolveNoise = GrassSimpleNoise(uv, _DissolveScale);

                float dissolveThreshold = 1.0 - _DissolveAmount;
                float outlineThreshold = dissolveThreshold + _OutlineThickness;

                visibleMask = step(dissolveNoise, outlineThreshold);
                float innerMask = step(dissolveNoise, dissolveThreshold);
                outlineMask = saturate(visibleMask - innerMask);
            }

            half4 ApplyGrassSurfaceEffects(half4 litColor, float2 uv)
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float visibleMask;
                float outlineMask;
                GetGrassDissolveMasks(uv, visibleMask, outlineMask);

                // Do not draw dissolve/outline in fully transparent parts of the atlas cell.
                float alphaPresence = step(0.001, baseSample.a);
                visibleMask *= alphaPresence;
                outlineMask *= alphaPresence;

                litColor.rgb = lerp(litColor.rgb, _OutlineColor.rgb, outlineMask);

                float3 dryColor = GrassHueShiftDegrees(litColor.rgb, 323.7);
                dryColor = GrassApplySaturation(dryColor, 0.7);
                litColor.rgb = lerp(litColor.rgb, dryColor, _Dryness);

                litColor.a *= visibleMask;
                return litColor;
            }


            Varyings UnlitVertex(Attributes input)
            {
                input.positionOS = ApplyGrassSway(input.positionOS, input.custom);
                Varyings output = CommonUnlitVertex(input);
                output.grassUV = input.uv;
                return output;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half4 unlitColor = CommonUnlitFragment(input, _White);
                return ApplyGrassSurfaceEffects(unlitColor, input.grassUV);
            }
            ENDHLSL
        }
    }
}
