Shader "Pit Striker/Village Foliage"
{
    Properties
    {
        _AmbientFill("Sky fill", Range(0,1)) = 0.5
        _BaseColor("Leaf color", Color) = (.38, .54, .14, 1)
        _BaseMap("Base (RGBA)", 2D) = "white" {}
        _Transmission("Sunlight through leaves", Range(0,1)) = 0.45
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.35
        _WindSpeed("Wind Speed", Float) = 1.8
        _WindStrength("Wind Strength", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _AmbientFill;
                float4 _BaseColor, _BaseMap_ST;
                float _Cutoff, _Transmission, _WindSpeed, _WindStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct A { float4 p:POSITION; float3 n:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float fog:TEXCOORD2; float2 uv:TEXCOORD3; half3 vertexLight:TEXCOORD6; float4 color:COLOR; };

            float3 WindPosition(float3 positionOS, float weight)
            {
                float3 w = TransformObjectToWorld(positionOS);
                float t = _Time.y * _WindSpeed;
                float wave1 = sin(w.z * 0.9 + w.x * 0.6 + t) * (_WindStrength * 0.9);
                float wave2 = sin(w.x * 1.8 - w.z * 1.2 + t * 2.1) * (_WindStrength * 0.45);
                float wave3 = sin(t * 3.4 + w.y * 2.0) * (_WindStrength * 0.25);
                w.x += (wave1 + wave2) * saturate(weight);
                w.y += wave3 * saturate(weight) * 0.3;
                w.z += (wave2 * 0.6 + wave1 * 0.4) * saturate(weight);
                return w;
            }

            V vert(A a)
            {
                V o;
                float3 w = WindPosition(a.p.xyz, a.color.a);
                o.color = a.color;
                o.world = w;
                o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
                o.p = TransformWorldToHClip(w);
                o.normal = TransformObjectToWorldNormal(a.n);
                o.fog = ComputeFogFactor(o.p.z);
                o.vertexLight = VertexLighting(o.world, o.normal);
                return o;
            }
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            half4 frag(V i, bool front:SV_IsFrontFace):SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);

                InputData d = (InputData)0;
                d.positionWS = i.world;
                half3 geomNormal = normalize(i.normal);
                d.normalWS = geomNormal * (front ? 1 : -1);
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.world);
                d.shadowCoord = TransformWorldToShadowCoord(i.world);
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.p.xy);
                d.shadowMask = half4(1,1,1,1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = _BaseColor.rgb * i.color.rgb * tex.rgb;
                s.smoothness = 0.18;
                s.occlusion = 1.0;
                s.alpha = tex.a;

                // Rich tropical foliage subsurface transmission:
                // Sun rays piercing through thin leaf membranes glow emerald/gold
                Light sun = GetMainLight(d.shadowCoord);
                half through = pow(saturate(dot(-sun.direction, d.viewDirectionWS) * 0.6 + 0.4), 2.2);
                half3 transmissionColor = s.albedo * half3(1.2, 1.25, 0.45); // Golden-green warm glow
                s.emission = transmissionColor * sun.color * (through * _Transmission) * sun.shadowAttenuation;

                // Warm sky and ground bounce fill
                d.bakedGI += _AmbientFill * lerp(half3(0.24, 0.22, 0.12), half3(0.55, 0.68, 0.82), saturate(d.normalWS.y * 0.5 + 0.5));
                d.vertexLighting = i.vertexLight;

                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
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

            struct ShadowV { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };

            float3 _LightDirection, _LightPosition;

            ShadowV shadowVert(A a)
            {
                ShadowV o;
                float3 w = WindPosition(a.p.xyz, a.color.a);
                float3 n = TransformObjectToWorldNormal(a.n);
                float3 lightDirection = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                lightDirection = normalize(_LightPosition - w);
                #endif
                o.p = TransformWorldToHClip(ApplyShadowBias(w, n, lightDirection));
                #if UNITY_REVERSED_Z
                o.p.z = min(o.p.z, UNITY_NEAR_CLIP_VALUE * o.p.w);
                #else
                o.p.z = max(o.p.z, UNITY_NEAR_CLIP_VALUE * o.p.w);
                #endif
                o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
                return o;
            }

            half4 shadowFrag(ShadowV i):SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);
                return 0;
            }
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

            struct DepthV { float4 p:SV_POSITION; float2 uv:TEXCOORD0; };

            DepthV depthVert(A a)
            {
                DepthV o;
                float3 w = WindPosition(a.p.xyz, a.color.a);
                o.p = TransformWorldToHClip(w);
                o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
                return o;
            }

            half4 depthFrag(DepthV i):SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(tex.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
