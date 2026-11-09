Shader "SWG/Character"
{
 Properties { _BaseMap("Diffuse",2D)="white"{} _Tint("Skin tint",Color)=(1,1,1,1) _Cutoff("Alpha cutoff",Range(0,1))=0 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1; };
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST;half4 _Tint;half _Cutoff; CBUFFER_END
   Varyings Vert(Attributes input) {Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=input.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;return o;}
   half4 Frag(Varyings input):SV_Target {Light light=GetMainLight();half3 normal=normalize(input.normalWS);half brightness=0.42+0.58*saturate(dot(normal,normalize(float3(-0.35,0.6,0.8))));half4 sampleColor=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);clip(sampleColor.a-_Cutoff);half3 color=sampleColor.rgb;return half4(color*_Tint.rgb*brightness,1);}
   ENDHLSL
  }
 }
}

