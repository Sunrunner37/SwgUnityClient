Shader "SWG/IntroTexture"
{
 Properties { _BaseMap("Texture",2D)="white"{} _Opacity("Opacity",Float)=1 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"} Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);CBUFFER_START(UnityPerMaterial) float _Opacity; CBUFFER_END
 V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;return v;}
 half4 Frag(V v):SV_Target{half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv);c.a*=saturate(max(c.r,max(c.g,c.b))*3)*_Opacity;return c;}
 ENDHLSL
 }}
}
