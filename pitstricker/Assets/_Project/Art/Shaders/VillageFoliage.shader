Shader "Pit Striker/Village Foliage"
{
    Properties
    {
        _BaseColor("Leaf color", Color) = (.3,.5,.06,1)
        _BaseMap("Base", 2D) = "white" {}
        _Cull("Cull", Float) = 0
        _Cutoff("Cutoff", Float) = .5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _BaseMap_ST; float _Cull, _Cutoff;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float fog:TEXCOORD2; };
            V vert(A a)
            {
                V o; float3 w=TransformObjectToWorld(a.p.xyz);
                w.x+=sin(w.z*1.1+w.x*.7+_Time.y*1.4)*.035*saturate(w.y*3);
                o.world=w; o.p=TransformWorldToHClip(w); o.normal=TransformObjectToWorldNormal(a.n); o.fog=ComputeFogFactor(o.p.z); return o;
            }
            half4 frag(V i, bool front:SV_IsFrontFace):SV_Target
            {
                InputData d=(InputData)0; d.positionWS=i.world;
                d.normalWS=normalize(i.normal)*(front?1:-1); d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
                d.shadowCoord=TransformWorldToShadowCoord(i.world); d.bakedGI=max(SampleSH(d.normalWS),half3(.26,.30,.17));
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p); d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0; s.albedo=_BaseColor.rgb; s.smoothness=.2; s.occlusion=1; s.alpha=1;
                s.emission=_BaseColor.rgb*.07;
                half4 c=UniversalFragmentPBR(d,s); c.rgb=MixFog(c.rgb,i.fog); return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
