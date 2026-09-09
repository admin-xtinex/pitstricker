Shader "Pit Striker/Village Foliage"
{
    Properties
    {
        _BaseColor("Leaf color", Color) = (.3,.5,.06,1)
        _BaseMap("Base", 2D) = "white" {}
        _Transmission("Sunlight through leaves", Range(0,1)) = .35
        _Cull("Cull", Float) = 0
        _Cutoff("Cutoff", Float) = .5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _BaseMap_ST; float _Cull, _Cutoff, _Transmission;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float fog:TEXCOORD2;half3 vertexLight:TEXCOORD6; };
            float3 WindPosition(float3 positionOS)
            {
                float3 w=TransformObjectToWorld(positionOS);
                w.x+=sin(w.z*1.1+w.x*.7+_Time.y*1.4)*.035*saturate(w.y*3);
                return w;
            }
            V vert(A a)
            {
                V o; float3 w=WindPosition(a.p.xyz);
                o.world=w; o.p=TransformWorldToHClip(w); o.normal=TransformObjectToWorldNormal(a.n); o.fog=ComputeFogFactor(o.p.z); o.vertexLight=VertexLighting(o.world,o.normal);return o;
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
            #pragma multi_compile_fog
            half4 frag(V i, bool front:SV_IsFrontFace):SV_Target
            {
                InputData d=(InputData)0; d.positionWS=i.world;
                d.normalWS=normalize(i.normal)*(front?1:-1); d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
                d.shadowCoord=TransformWorldToShadowCoord(i.world); d.bakedGI=SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p); d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0; s.albedo=_BaseColor.rgb; s.smoothness=.2; s.occlusion=1; s.alpha=1;
                // Backlighting follows the sun and its shadow rather than glowing in shade.
                Light sun=GetMainLight(d.shadowCoord);
                half through=pow(saturate(dot(-sun.direction,d.viewDirectionWS)),3);
                s.emission=_BaseColor.rgb*sun.color*through*_Transmission*sun.shadowAttenuation;
                d.vertexLighting=i.vertexLight;
                half4 c=UniversalFragmentPBR(d,s); c.rgb=MixFog(c.rgb,i.fog); return c;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadow
            #pragma fragment depth
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            float4 shadow(A a):SV_POSITION
            {
                float3 w=WindPosition(a.p.xyz);
                float3 n=TransformObjectToWorldNormal(a.n);
                float3 lightDirection=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                lightDirection=normalize(_LightPosition-w);
                #endif
                float4 p=TransformWorldToHClip(ApplyShadowBias(w,n,lightDirection));
                #if UNITY_REVERSED_Z
                p.z=min(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #else
                p.z=max(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #endif
                return p;
            }
            half4 depth():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depth
            half4 depth():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
