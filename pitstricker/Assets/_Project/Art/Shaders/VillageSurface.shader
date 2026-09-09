Shader "Pit Striker/Village Surface"
{
    Properties
    {
        _BaseColor("Color",Color)=(1,1,1,1)
        _BaseMap("Surface texture",2D)="white"{}
        _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Normal strength",Float)=.6
        _Smoothness("Smoothness",Range(0,1))=.2
        _Metallic("Metallic",Range(0,1))=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_BaseMap_ST;
            float _BumpScale,_Smoothness,_Metallic;
        CBUFFER_END
        struct A {float4 p:POSITION;float3 n:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;};
        struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;float fog:TEXCOORD3;half3 vertexLight:TEXCOORD6;float3 t:TEXCOORD4;float3 b:TEXCOORD5;};
        V vert(A a)
        {
            V o; VertexPositionInputs p=GetVertexPositionInputs(a.p.xyz); VertexNormalInputs n=GetVertexNormalInputs(a.n,a.tangent);
            o.p=p.positionCS;o.world=p.positionWS;o.n=n.normalWS;o.t=n.tangentWS;o.b=n.bitangentWS;o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(o.p.z);o.vertexLight=VertexLighting(o.world,o.n);return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardSurface"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            half4 frag(V i):SV_Target
            {
                half3 n=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=normalize(n.x*i.t+n.y*i.b+n.z*i.n);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);
                d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=1;
                SurfaceData s=(SurfaceData)0;s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;s.smoothness=_Smoothness;s.metallic=_Metallic;s.occlusion=1;s.alpha=1;
                d.vertexLighting=i.vertexLight;
                half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadow
            #pragma fragment depth
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            float4 shadow(A a):SV_POSITION
            {
                float3 w=TransformObjectToWorld(a.p.xyz),n=TransformObjectToWorldNormal(a.n);
                float3 l=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                l=normalize(_LightPosition-w);
                #endif
                float4 p=TransformWorldToHClip(ApplyShadowBias(w,n,l));
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
            Tags {"LightMode"="DepthOnly"}
            ColorMask 0 ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depth
            half4 depth():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
