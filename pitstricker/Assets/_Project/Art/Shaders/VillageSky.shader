Shader "Pit Striker/Village Sky"
{
    Properties { _SunDirection("Sun direction", Vector)=(.62,.54,-.56,0) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _SunDirection;
            CBUFFER_END
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 direction:TEXCOORD0; };
            V vert(A a) { V o; o.p=TransformObjectToHClip(a.p.xyz); o.direction=a.p.xyz; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);
                half3 sky=lerp(half3(.72,.81,.86),half3(.16,.40,.72),pow(saturate(d.y),.45));
                float2 p=d.xz/max(d.y+.13,.06)*1.7;
                float n=noise(p)*.55+noise(p*2.1)*.27+noise(p*4.3)*.12+noise(p*8.5)*.06;
                float cloud=smoothstep(.48,.68,n)*smoothstep(.02,.18,d.y);
                sky=lerp(sky,lerp(half3(.74,.77,.79),half3(1,.96,.85),n),cloud*.9);
                float sun=pow(saturate(dot(d,normalize(_SunDirection.xyz))),900);
                sky+=half3(1,.85,.6)*sun*2;
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
