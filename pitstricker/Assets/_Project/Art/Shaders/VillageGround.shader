Shader "Pit Striker/Village Ground"
{
    Properties
    {
        _BaseMap("Soil detail", 2D) = "white" {}
        _BaseColor("Tint", Color) = (0.7,0.5,0.3,1)
        _BumpMap("Soil normal", 2D) = "bump" {}
        _BumpScale("Relief", Float) = 0.75
        _WorldScale("Repeats per metre", Float) = 0.65
        _Smoothness("Smoothness", Range(0,1)) = 0.15
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
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST;
                float _BumpScale, _WorldScale, _Smoothness, _Cull, _Cutoff;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float fog:TEXCOORD2; };
            V vert(A a)
            {
                V o; VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);
                o.positionCS=p.positionCS; o.world=p.positionWS;
                o.normal=TransformObjectToWorldNormal(a.normalOS); o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 frag(V i):SV_Target
            {
                float2 uv=i.world.xz*_WorldScale;
                half3 detail=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                half3 broad=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*.19+float2(.31,.13)).rgb;
                half3 n=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv),_BumpScale);
                InputData d=(InputData)0;
                d.positionWS=i.world; d.normalWS=normalize(i.normal+float3(n.x,0,n.y)*.6);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
                d.shadowCoord=TransformWorldToShadowCoord(i.world); d.bakedGI=SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask=half4(1,1,1,1);
                // Keep the photographed soil relief while recoloring the red clay
                // source to the reference's dry ochre lane.
                half grain=dot(detail,half3(.3,.59,.11));
                half hx=dot(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+float2(.002,0)).rgb,half3(.3,.59,.11));
                half hz=dot(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+float2(0,.002)).rgb,half3(.3,.59,.11));
                d.normalWS=normalize(d.normalWS+float3(grain-hx,0,grain-hz)*2.5);
                SurfaceData s=(SurfaceData)0; s.albedo=detail*_BaseColor.rgb*lerp(.9,1.1,broad.r);
                d.bakedGI=max(d.bakedGI,half3(.14,.16,.18));
                s.alpha=1; s.occlusion=1; s.smoothness=_Smoothness; s.specular=half3(.04,.04,.04);
                half4 c=UniversalFragmentPBR(d,s); c.rgb=MixFog(c.rgb,i.fog); return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
