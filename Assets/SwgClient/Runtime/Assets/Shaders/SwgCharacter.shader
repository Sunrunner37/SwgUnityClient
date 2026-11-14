Shader "SWG/Character"
{
    Properties
    {
        _SwgPreview("UI preview lighting", Float) = 0
        _BaseMap("Diffuse", 2D) = "white" {}
        _Tint("Skin tint", Color) = (1,1,1,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "SwgShadows.hlsl"
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float3 worldPosition : TEXCOORD2; half fog : TEXCOORD3; };
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Tint;
            half _Cutoff;
            float _SwgPreview;
        CBUFFER_END
        float _SwgEnvironmentLighting;
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.worldPosition = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.worldPosition);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half4 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(surface.a - _Cutoff);
                half brightness = .42 + .58 * saturate(dot(normal, normalize(float3(-.35,.6,.8))));
                Light light = SwgMainLight(input.worldPosition);
                half3 lighting = max(SampleSH(normal), half3(0,0,0)) + light.color * saturate(dot(normal, light.direction)) * light.shadowAttenuation * .75;
                half3 color = surface.rgb * _Tint.rgb * lerp(half3(brightness,brightness,brightness), lighting, _SwgEnvironmentLighting * (1 - _SwgPreview));
                return half4(lerp(color, MixFog(color, input.fog), _SwgEnvironmentLighting * (1 - _SwgPreview)), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            ShadowVaryings ShadowVert(Attributes input)
            {
                Varyings vertex = Vert(input);
                ShadowVaryings output;
                output.positionCS = SwgShadowPosition(vertex.worldPosition, vertex.normalWS);
                output.uv = vertex.uv;
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
