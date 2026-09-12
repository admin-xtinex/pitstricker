Shader "Pit Striker/Stylized Toon PBR"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.55
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Occlusion("Occlusion Strength", Range(0, 1)) = 1.0
        _RimColor("Rim Color", Color) = (1, 0.95, 0.8, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.5
        _RimIntensity("Rim Intensity", Range(0, 2.0)) = 0.45
        _Wrap("Diffuse Wrap", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _BaseMap_ST, _RimColor;
                float _BumpScale, _Smoothness, _Metallic, _Occlusion;
                float _RimPower, _RimIntensity, _Wrap;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 color        : TEXCOORD3;
                float fogFactor     : TEXCOORD4;
                float3 tangentWS    : TEXCOORD5;
                float3 bitangentWS  : TEXCOORD6;
                half3  vertexLight  : TEXCOORD7;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                output.vertexLight = VertexLighting(output.positionWS, output.normalWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 normalWS = normalize(normalTS.x * input.tangentWS + normalTS.y * input.bitangentWS + normalTS.z * input.normalWS);
                if (dot(normalWS, normalWS) < 0.01) normalWS = normalize(input.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS.xy);
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.vertexLighting = input.vertexLight;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = texColor.rgb * input.color.rgb;
                surfaceData.smoothness = _Smoothness;
                surfaceData.metallic = _Metallic;
                surfaceData.occlusion = _Occlusion;
                surfaceData.alpha = texColor.a;

                // Stylized Cartoon Fresnel Rim
                float nDotV = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rim = pow(nDotV, _RimPower) * _RimIntensity;
                surfaceData.emission = _RimColor.rgb * rim;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                // Diffuse wrap softening for cartoon lighting
                Light mainLight = GetMainLight(inputData.shadowCoord);
                float halfLambert = saturate((dot(normalWS, mainLight.direction) + _Wrap) / (1.0 + _Wrap));
                half3 softLight = mainLight.color * (halfLambert * 0.15h);
                finalColor.rgb += softLight * surfaceData.albedo;

                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attr { float4 p : POSITION; float3 n : NORMAL; };
            float3 _LightDirection, _LightPosition;

            float4 shadowVert(Attr a) : SV_POSITION
            {
                float3 w = TransformObjectToWorld(a.p.xyz);
                float3 n = TransformObjectToWorldNormal(a.n);
                float3 l = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                l = normalize(_LightPosition - w);
                #endif
                float4 p = TransformWorldToHClip(ApplyShadowBias(w, n, l));
                #if UNITY_REVERSED_Z
                p.z = min(p.z, UNITY_NEAR_CLIP_VALUE * p.w);
                #else
                p.z = max(p.z, UNITY_NEAR_CLIP_VALUE * p.w);
                #endif
                return p;
            }
            half4 shadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attr { float4 p : POSITION; };
            float4 depthVert(Attr a) : SV_POSITION { return TransformObjectToHClip(a.p.xyz); }
            half4 depthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
