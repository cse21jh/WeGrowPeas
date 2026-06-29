Shader "Shader Graphs/SwayEffect"
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
        [HideInInspector]White("Color", Color) = (1, 1, 1, 1)
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Lit"
            "Queue"="Transparent"
            // DisableBatching: <None>
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalSpriteLitSubTarget"
        }
        Pass
        {
            Name "Sprite Lit"
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_0
        #pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_1
        #pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_2
        #pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_vertex _ SKINNED_SPRITE
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_COLOR
        #define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_COLOR
        #define VARYINGS_NEED_SCREENPOSITION
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SPRITELIT
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 color : COLOR;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 texCoord0;
             float4 color;
             float4 screenPosition;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 color : INTERP1;
             float4 screenPosition : INTERP2;
             float3 positionWS : INTERP3;
             float3 normalWS : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.color.xyzw = input.color;
            output.screenPosition.xyzw = input.screenPosition;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.color = input.color.xyzw;
            output.screenPosition = input.screenPosition.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _DissolveAmount;
        float _OutlineThickness;
        float _DissolveScale;
        float4 _MainTex_TexelSize;
        float _SwayPower;
        float _WindSpeed;
        float _Dryness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), (IN.TimeParameters.x.xx), _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2);
            float _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float = _SwayPower;
            float _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2, _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float, _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float);
            float _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float;
            Unity_Subtract_float(_GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float, float(0.5), _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float);
            float4 _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4 = IN.uv0;
            float _Split_8515b19ce2824a14955904edd8ae84a1_R_1_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[0];
            float _Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[1];
            float _Split_8515b19ce2824a14955904edd8ae84a1_B_3_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[2];
            float _Split_8515b19ce2824a14955904edd8ae84a1_A_4_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[3];
            float _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float;
            Unity_Preview_float(_Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float);
            float _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float;
            Unity_Multiply_float_float(_Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float, _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float);
            float _Split_478e9a0af023440284ebe875708a3ded_R_1_Float = IN.ObjectSpacePosition[0];
            float _Split_478e9a0af023440284ebe875708a3ded_G_2_Float = IN.ObjectSpacePosition[1];
            float _Split_478e9a0af023440284ebe875708a3ded_B_3_Float = IN.ObjectSpacePosition[2];
            float _Split_478e9a0af023440284ebe875708a3ded_A_4_Float = 0;
            float _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float;
            Unity_Add_float(_Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_R_1_Float, _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float);
            float4 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4;
            float3 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3;
            float2 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2;
            Unity_Combine_float(_Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_G_2_Float, _Split_478e9a0af023440284ebe875708a3ded_B_3_Float, float(0), _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2);
            description.Position = (_Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4.xyz);
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float4 SpriteMask;
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.tex, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.samplerstate, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_R_4_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.r;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_G_5_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.g;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_B_6_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.b;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.a;
            float _Property_b98a2d180a394d438f657bc139008781_Out_0_Float = _DissolveScale;
            float _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(IN.uv0.xy, _Property_b98a2d180a394d438f657bc139008781_Out_0_Float, _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float);
            float _Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float = _DissolveAmount;
            float _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float;
            Unity_OneMinus_float(_Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float);
            float _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float = _OutlineThickness;
            float _Add_9049064d961c45e89b0e256623273106_Out_2_Float;
            Unity_Add_float(_OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float);
            float _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float);
            float _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float);
            float _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float;
            Unity_Subtract_float(_Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float, _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float);
            float4 _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4;
            Unity_Subtract_float4(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4, (_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4);
            float4 Color_3182d854346f471e9d977980e2f0f667 = IsGammaSpace() ? float4(0.1924528, 0.1141578, 0.09114272, 1) : float4(SRGBToLinear(float3(0.1924528, 0.1141578, 0.09114272)), 1);
            float4 _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4;
            Unity_Multiply_float4_float4((_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), Color_3182d854346f471e9d977980e2f0f667, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4);
            float4 _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4;
            Unity_Add_float4(_Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4, _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4);
            float3 _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3;
            Unity_Hue_Degrees_float((_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), float(323.7), _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3);
            float3 _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3;
            Unity_Saturation_float(_Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3, float(0.7), _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3);
            float _Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float = _Dryness;
            float _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float;
            Unity_OneMinus_float(_Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float, _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float);
            float3 _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            Unity_Lerp_float3(_Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3, (_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), (_OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float.xxx), _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3);
            float _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float);
            surface.BaseColor = _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            surface.SpriteMask = IsGammaSpace() ? float4(1, 1, 1, 1) : float4 (SRGBToLinear(float3(1, 1, 1)), 1);
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Alpha = _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteLitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Sprite Normal"
            Tags
            {
                "LightMode" = "NormalsRendering"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ SKINNED_SPRITE
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_COLOR
        #define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_COLOR
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SPRITENORMAL
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 color : COLOR;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 color;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float4 color : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.color.xyzw = input.color;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.color = input.color.xyzw;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _DissolveAmount;
        float _OutlineThickness;
        float _DissolveScale;
        float4 _MainTex_TexelSize;
        float _SwayPower;
        float _WindSpeed;
        float _Dryness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), (IN.TimeParameters.x.xx), _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2);
            float _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float = _SwayPower;
            float _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2, _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float, _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float);
            float _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float;
            Unity_Subtract_float(_GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float, float(0.5), _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float);
            float4 _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4 = IN.uv0;
            float _Split_8515b19ce2824a14955904edd8ae84a1_R_1_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[0];
            float _Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[1];
            float _Split_8515b19ce2824a14955904edd8ae84a1_B_3_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[2];
            float _Split_8515b19ce2824a14955904edd8ae84a1_A_4_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[3];
            float _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float;
            Unity_Preview_float(_Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float);
            float _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float;
            Unity_Multiply_float_float(_Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float, _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float);
            float _Split_478e9a0af023440284ebe875708a3ded_R_1_Float = IN.ObjectSpacePosition[0];
            float _Split_478e9a0af023440284ebe875708a3ded_G_2_Float = IN.ObjectSpacePosition[1];
            float _Split_478e9a0af023440284ebe875708a3ded_B_3_Float = IN.ObjectSpacePosition[2];
            float _Split_478e9a0af023440284ebe875708a3ded_A_4_Float = 0;
            float _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float;
            Unity_Add_float(_Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_R_1_Float, _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float);
            float4 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4;
            float3 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3;
            float2 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2;
            Unity_Combine_float(_Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_G_2_Float, _Split_478e9a0af023440284ebe875708a3ded_B_3_Float, float(0), _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2);
            description.Position = (_Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4.xyz);
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.tex, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.samplerstate, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_R_4_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.r;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_G_5_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.g;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_B_6_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.b;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.a;
            float _Property_b98a2d180a394d438f657bc139008781_Out_0_Float = _DissolveScale;
            float _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(IN.uv0.xy, _Property_b98a2d180a394d438f657bc139008781_Out_0_Float, _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float);
            float _Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float = _DissolveAmount;
            float _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float;
            Unity_OneMinus_float(_Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float);
            float _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float = _OutlineThickness;
            float _Add_9049064d961c45e89b0e256623273106_Out_2_Float;
            Unity_Add_float(_OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float);
            float _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float);
            float _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float);
            float _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float;
            Unity_Subtract_float(_Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float, _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float);
            float4 _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4;
            Unity_Subtract_float4(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4, (_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4);
            float4 Color_3182d854346f471e9d977980e2f0f667 = IsGammaSpace() ? float4(0.1924528, 0.1141578, 0.09114272, 1) : float4(SRGBToLinear(float3(0.1924528, 0.1141578, 0.09114272)), 1);
            float4 _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4;
            Unity_Multiply_float4_float4((_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), Color_3182d854346f471e9d977980e2f0f667, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4);
            float4 _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4;
            Unity_Add_float4(_Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4, _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4);
            float3 _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3;
            Unity_Hue_Degrees_float((_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), float(323.7), _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3);
            float3 _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3;
            Unity_Saturation_float(_Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3, float(0.7), _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3);
            float _Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float = _Dryness;
            float _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float;
            Unity_OneMinus_float(_Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float, _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float);
            float3 _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            Unity_Lerp_float3(_Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3, (_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), (_OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float.xxx), _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3);
            float _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float);
            surface.BaseColor = _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Alpha = _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteNormalPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _DissolveAmount;
        float _OutlineThickness;
        float _DissolveScale;
        float4 _MainTex_TexelSize;
        float _SwayPower;
        float _WindSpeed;
        float _Dryness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), (IN.TimeParameters.x.xx), _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2);
            float _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float = _SwayPower;
            float _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2, _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float, _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float);
            float _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float;
            Unity_Subtract_float(_GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float, float(0.5), _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float);
            float4 _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4 = IN.uv0;
            float _Split_8515b19ce2824a14955904edd8ae84a1_R_1_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[0];
            float _Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[1];
            float _Split_8515b19ce2824a14955904edd8ae84a1_B_3_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[2];
            float _Split_8515b19ce2824a14955904edd8ae84a1_A_4_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[3];
            float _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float;
            Unity_Preview_float(_Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float);
            float _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float;
            Unity_Multiply_float_float(_Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float, _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float);
            float _Split_478e9a0af023440284ebe875708a3ded_R_1_Float = IN.ObjectSpacePosition[0];
            float _Split_478e9a0af023440284ebe875708a3ded_G_2_Float = IN.ObjectSpacePosition[1];
            float _Split_478e9a0af023440284ebe875708a3ded_B_3_Float = IN.ObjectSpacePosition[2];
            float _Split_478e9a0af023440284ebe875708a3ded_A_4_Float = 0;
            float _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float;
            Unity_Add_float(_Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_R_1_Float, _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float);
            float4 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4;
            float3 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3;
            float2 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2;
            Unity_Combine_float(_Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_G_2_Float, _Split_478e9a0af023440284ebe875708a3ded_B_3_Float, float(0), _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2);
            description.Position = (_Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4.xyz);
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.tex, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.samplerstate, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_R_4_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.r;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_G_5_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.g;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_B_6_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.b;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.a;
            float _Property_b98a2d180a394d438f657bc139008781_Out_0_Float = _DissolveScale;
            float _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(IN.uv0.xy, _Property_b98a2d180a394d438f657bc139008781_Out_0_Float, _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float);
            float _Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float = _DissolveAmount;
            float _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float;
            Unity_OneMinus_float(_Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float);
            float _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float = _OutlineThickness;
            float _Add_9049064d961c45e89b0e256623273106_Out_2_Float;
            Unity_Add_float(_OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float);
            float _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float);
            float _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float);
            surface.Alpha = _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        
        #define _ALPHATEST_ON 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _DissolveAmount;
        float _OutlineThickness;
        float _DissolveScale;
        float4 _MainTex_TexelSize;
        float _SwayPower;
        float _WindSpeed;
        float _Dryness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), (IN.TimeParameters.x.xx), _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2);
            float _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float = _SwayPower;
            float _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2, _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float, _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float);
            float _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float;
            Unity_Subtract_float(_GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float, float(0.5), _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float);
            float4 _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4 = IN.uv0;
            float _Split_8515b19ce2824a14955904edd8ae84a1_R_1_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[0];
            float _Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[1];
            float _Split_8515b19ce2824a14955904edd8ae84a1_B_3_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[2];
            float _Split_8515b19ce2824a14955904edd8ae84a1_A_4_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[3];
            float _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float;
            Unity_Preview_float(_Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float);
            float _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float;
            Unity_Multiply_float_float(_Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float, _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float);
            float _Split_478e9a0af023440284ebe875708a3ded_R_1_Float = IN.ObjectSpacePosition[0];
            float _Split_478e9a0af023440284ebe875708a3ded_G_2_Float = IN.ObjectSpacePosition[1];
            float _Split_478e9a0af023440284ebe875708a3ded_B_3_Float = IN.ObjectSpacePosition[2];
            float _Split_478e9a0af023440284ebe875708a3ded_A_4_Float = 0;
            float _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float;
            Unity_Add_float(_Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_R_1_Float, _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float);
            float4 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4;
            float3 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3;
            float2 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2;
            Unity_Combine_float(_Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_G_2_Float, _Split_478e9a0af023440284ebe875708a3ded_B_3_Float, float(0), _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2);
            description.Position = (_Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4.xyz);
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.tex, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.samplerstate, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_R_4_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.r;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_G_5_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.g;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_B_6_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.b;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.a;
            float _Property_b98a2d180a394d438f657bc139008781_Out_0_Float = _DissolveScale;
            float _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(IN.uv0.xy, _Property_b98a2d180a394d438f657bc139008781_Out_0_Float, _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float);
            float _Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float = _DissolveAmount;
            float _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float;
            Unity_OneMinus_float(_Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float);
            float _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float = _OutlineThickness;
            float _Add_9049064d961c45e89b0e256623273106_Out_2_Float;
            Unity_Add_float(_OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float);
            float _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float);
            float _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float);
            surface.Alpha = _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Sprite Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZTest LEqual
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma exclude_renderers d3d11_9x
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_vertex _ SKINNED_SPRITE
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_COLOR
        #define GRAPH_VERTEX_USES_TIME_PARAMETERS_INPUT
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_COLOR
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SPRITEFORWARD
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 color : COLOR;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float4 texCoord0;
             float4 color;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 color : INTERP1;
             float3 positionWS : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.color.xyzw = input.color;
            output.positionWS.xyz = input.positionWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.color = input.color.xyzw;
            output.positionWS = input.positionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _DissolveAmount;
        float _OutlineThickness;
        float _DissolveScale;
        float4 _MainTex_TexelSize;
        float _SwayPower;
        float _WindSpeed;
        float _Dryness;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        float Unity_SimpleNoise_ValueNoise_Deterministic_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0; Hash_Tchou_2_1_float(c0, r0);
            float r1; Hash_Tchou_2_1_float(c1, r1);
            float r2; Hash_Tchou_2_1_float(c2, r2);
            float r3; Hash_Tchou_2_1_float(c3, r3);
            float bottomOfGrid = lerp(r0, r1, f.x);
            float topOfGrid = lerp(r2, r3, f.x);
            float t = lerp(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        
        void Unity_SimpleNoise_Deterministic_float(float2 UV, float Scale, out float Out)
        {
            float freq, amp;
            Out = 0.0f;
            freq = pow(2.0, float(0));
            amp = pow(0.5, float(3-0));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            Out += Unity_SimpleNoise_ValueNoise_Deterministic_float(float2(UV.xy*(Scale/freq)))*amp;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Hue_Degrees_float(float3 In, float Offset, out float3 Out)
        {
            // RGB to HSV
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
            float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
            float D = Q.x - min(Q.w, Q.y);
            float E = 1e-10;
            float V = (D == 0) ? Q.x : (Q.x + E);
            float3 hsv = float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
        
            float hue = hsv.x + Offset / 360;
            hsv.x = (hue < 0)
                    ? hue + 1
                    : (hue > 1)
                        ? hue - 1
                        : hue;
        
            // HSV to RGB
            float4 K2 = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
            Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
        }
        
        void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
        {
            float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
            Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2;
            Unity_TilingAndOffset_float((IN.WorldSpacePosition.xy), float2 (1, 1), (IN.TimeParameters.x.xx), _TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2);
            float _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float = _SwayPower;
            float _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_TilingAndOffset_9090b798243e4ed593f0392b072b81ec_Out_3_Vector2, _Property_2cd072ae0807476783da6ccf9ae141bd_Out_0_Float, _GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float);
            float _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float;
            Unity_Subtract_float(_GradientNoise_191e67b015164ec196871e5081662e6e_Out_2_Float, float(0.5), _Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float);
            float4 _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4 = IN.uv0;
            float _Split_8515b19ce2824a14955904edd8ae84a1_R_1_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[0];
            float _Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[1];
            float _Split_8515b19ce2824a14955904edd8ae84a1_B_3_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[2];
            float _Split_8515b19ce2824a14955904edd8ae84a1_A_4_Float = _UV_bb344eedffdd4b12b329f35de355223e_Out_0_Vector4[3];
            float _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float;
            Unity_Preview_float(_Split_8515b19ce2824a14955904edd8ae84a1_G_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float);
            float _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float;
            Unity_Multiply_float_float(_Subtract_f21096323b5344fdbdfd09bc4ae48808_Out_2_Float, _Preview_1da7d5bdcb63448295a5028b24a69064_Out_1_Float, _Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float);
            float _Split_478e9a0af023440284ebe875708a3ded_R_1_Float = IN.ObjectSpacePosition[0];
            float _Split_478e9a0af023440284ebe875708a3ded_G_2_Float = IN.ObjectSpacePosition[1];
            float _Split_478e9a0af023440284ebe875708a3ded_B_3_Float = IN.ObjectSpacePosition[2];
            float _Split_478e9a0af023440284ebe875708a3ded_A_4_Float = 0;
            float _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float;
            Unity_Add_float(_Multiply_365b440a24a84fb781275b82873d0f77_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_R_1_Float, _Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float);
            float4 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4;
            float3 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3;
            float2 _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2;
            Unity_Combine_float(_Add_5d3d38e6f6774b14a3f9b726bc74ea8d_Out_2_Float, _Split_478e9a0af023440284ebe875708a3ded_G_2_Float, _Split_478e9a0af023440284ebe875708a3ded_B_3_Float, float(0), _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RGB_5_Vector3, _Combine_0b64cb90041149fa9d931ceea2c45fc0_RG_6_Vector2);
            description.Position = (_Combine_0b64cb90041149fa9d931ceea2c45fc0_RGBA_4_Vector4.xyz);
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.tex, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.samplerstate, _Property_ecccd7aee46847cbabfa002f0af7ca7d_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_R_4_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.r;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_G_5_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.g;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_B_6_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.b;
            float _SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float = _SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4.a;
            float _Property_b98a2d180a394d438f657bc139008781_Out_0_Float = _DissolveScale;
            float _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float;
            Unity_SimpleNoise_Deterministic_float(IN.uv0.xy, _Property_b98a2d180a394d438f657bc139008781_Out_0_Float, _SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float);
            float _Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float = _DissolveAmount;
            float _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float;
            Unity_OneMinus_float(_Property_cb042abcb3484ad0902a0c996325c65e_Out_0_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float);
            float _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float = _OutlineThickness;
            float _Add_9049064d961c45e89b0e256623273106_Out_2_Float;
            Unity_Add_float(_OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Property_9f974be2bec54e979af119b63f53ca16_Out_0_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float);
            float _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _Add_9049064d961c45e89b0e256623273106_Out_2_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float);
            float _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float;
            Unity_Step_float(_SimpleNoise_23f6e4a593de4a03914956cf0a509816_Out_2_Float, _OneMinus_bd772ff077b8442eab2b81169ae86424_Out_1_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float);
            float _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float;
            Unity_Subtract_float(_Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Step_b418ede1bac64a529172f60f04982aaa_Out_2_Float, _Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float);
            float4 _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4;
            Unity_Subtract_float4(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_RGBA_0_Vector4, (_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), _Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4);
            float4 Color_3182d854346f471e9d977980e2f0f667 = IsGammaSpace() ? float4(0.1924528, 0.1141578, 0.09114272, 1) : float4(SRGBToLinear(float3(0.1924528, 0.1141578, 0.09114272)), 1);
            float4 _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4;
            Unity_Multiply_float4_float4((_Subtract_9b112adc27444c909abba650bedf0e9e_Out_2_Float.xxxx), Color_3182d854346f471e9d977980e2f0f667, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4);
            float4 _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4;
            Unity_Add_float4(_Subtract_9fdca9e5a46945839ffd704cd59016ff_Out_2_Vector4, _Multiply_a8a85906c5ad4bd781f9c25c796f501c_Out_2_Vector4, _Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4);
            float3 _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3;
            Unity_Hue_Degrees_float((_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), float(323.7), _Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3);
            float3 _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3;
            Unity_Saturation_float(_Hue_12aa2211135a468ab6699230c2b6b423_Out_2_Vector3, float(0.7), _Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3);
            float _Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float = _Dryness;
            float _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float;
            Unity_OneMinus_float(_Property_985964a2dfd941b98846564b56b9fb59_Out_0_Float, _OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float);
            float3 _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            Unity_Lerp_float3(_Saturation_2eae2ffb68bd4e859d63d050c53df233_Out_2_Vector3, (_Add_a05ac14a0aab4246b3ba786a96d7d692_Out_2_Vector4.xyz), (_OneMinus_66f97e04d8354a4a92a13c6cbe6a1d2c_Out_1_Float.xxx), _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3);
            float _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_269d5dd126c34438bcade872cb703f26_A_7_Float, _Step_71a7632dca5f43a59d81ebcad49c62e8_Out_2_Float, _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float);
            surface.BaseColor = _Lerp_42f6f405bcd64f9f8260506f66ca4ddd_Out_3_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Alpha = _Multiply_c26041d79afc4bfa963c99d5badfb046_Out_2_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.WorldSpacePosition =                         TransformObjectToWorld(input.positionOS);
            output.uv0 =                                        input.uv0;
            output.TimeParameters =                             _TimeParameters.xyz;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/SpriteForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphSpriteGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}