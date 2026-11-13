Shader "SWG/Particle"
{
 Properties { _BaseMap("Texture", 2D) = "white" {} _DstBlend("Destination blend", Float) = 10 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha [_DstBlend]
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float _DstBlend;
 CBUFFER_END
 struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
 struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; half fog:TEXCOORD1; };
 Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.positionCS.z); return o; }
 half4 frag(Varyings v):SV_Target { half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv)*v.color; color.rgb=_DstBlend == 1 ? MixFogColor(color.rgb, half3(0,0,0), v.fog) : MixFog(color.rgb,v.fog); return color; }
 ENDHLSL
 }
 }
}
