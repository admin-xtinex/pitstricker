Shader "Pit Striker/Swirl Marble"
{
    Properties
    {
        _BaseColor("Glass body", Color) = (0.015,0.05,0.4,1)
        _VeinColor("Swirl ribbon", Color) = (0.12,0.6,1,1)
        _Smoothness("Polish", Range(0,1)) = 0.96
        _BaseMap("Base", 2D) = "white" {}
        _Cull("Cull", Float) = 2
        _Cutoff("Cutoff", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
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
                float4 _BaseColor, _VeinColor, _BaseMap_ST;
                float _Smoothness, _Cull, _Cutoff;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float3 local:TEXCOORD2; float fog:TEXCOORD3; };
            V vert(A a)
            {
                V o; VertexPositionInputs p=GetVertexPositionInputs(a.p.xyz); o.p=p.positionCS; o.world=p.positionWS;
                o.normal=TransformObjectToWorldNormal(a.n); o.local=a.n; o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 frag(V i):SV_Target
            {
                float3 q=normalize(i.local);
                float wave=sin(q.x*15+q.y*12+sin(q.z*9+q.x*5)*2.8);
                float ribbon=smoothstep(.28,.65,wave)*(.5+.5*sin(q.y*4+q.z*6));
                float fine=pow(saturate(sin(q.x*32+q.y*27+sin(q.z*9)*5)),12);
                InputData d=(InputData)0; d.positionWS=i.world; d.normalWS=normalize(i.normal);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world); d.shadowCoord=TransformWorldToShadowCoord(i.world);
                d.bakedGI=SampleSH(d.normalWS); d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p); d.shadowMask=half4(1,1,1,1);
                float rim=pow(1-saturate(dot(d.normalWS,d.viewDirectionWS)),3);
                SurfaceData s=(SurfaceData)0; s.albedo=lerp(_BaseColor.rgb,_VeinColor.rgb,ribbon*.8+fine*.12);
                s.smoothness=_Smoothness; s.metallic=.28; s.occlusion=1; s.alpha=1;
                s.emission=_VeinColor.rgb*(ribbon*.08+rim*.14);
                half4 c=UniversalFragmentPBR(d,s); c.rgb=MixFog(c.rgb,i.fog); return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
