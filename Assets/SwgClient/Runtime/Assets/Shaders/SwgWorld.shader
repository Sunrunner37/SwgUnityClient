Shader "SWG/World"
{
    Properties
    {
        _BaseMap("Diffuse", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = 0
        _UseVertexColor("Use vertex color", Float) = 0
        _Billboard("Billboard", Float) = 0
        _SwayDisplacement("Sway displacement", Float) = 0
        _SwayPeriod("Sway period", Float) = 1
        _SwayHeight("Sway height", Float) = 1
        _FloraMinimumDistance("Flora minimum distance", Float) = 0
        _FloraMaximumDistance("Flora maximum distance", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SwgShadows.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; float3 worldPosition:TEXCOORD3; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Tint;
            half _Cutoff;
            half _UseVertexColor;
            float _Billboard;
            float _SwayDisplacement;
            float _SwayPeriod;
            float _SwayHeight;
            float _FloraMinimumDistance;
            float _FloraMaximumDistance;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 position = input.positionOS.xyz;
                float3 center = TransformObjectToWorld(float3(0,0,0));
                float phase = _Time.y * _SwayPeriod + center.x + center.z;
                position.x += sin(phase) * _SwayDisplacement * saturate(position.y / max(.01, _SwayHeight));
                float3 worldPosition = TransformObjectToWorld(position);
                if (_Billboard > .5)
                {
                    float3 view = GetCameraPositionWS() - center;
                    float3 up = normalize(GetObjectToWorldMatrix()._m01_m11_m21);
                    float3 right = normalize(cross(up, view) + float3(.00001,0,0));
                    float scaleX = length(GetObjectToWorldMatrix()._m00_m10_m20);
                    float scaleY = length(GetObjectToWorldMatrix()._m01_m11_m21);
                    worldPosition = center + right * position.x * scaleX + up * position.y * scaleY;
                }
                output.worldPosition = worldPosition;
                output.positionCS = TransformWorldToHClip(worldPosition);
                output.normalWS = _Billboard > .5 ? normalize(GetCameraPositionWS() - center) : TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.color = input.color;
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input):SV_Target
            {
                if (_FloraMaximumDistance > 0)
                {
                    float distanceToCamera = distance(GetCameraPositionWS(), input.worldPosition);
                    clip(_FloraMaximumDistance - distanceToCamera);
                    clip(distanceToCamera - _FloraMinimumDistance);
                }
                half4 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(surface.a - _Cutoff);
                Light sun = SwgMainLight(input.worldPosition);
                half3 normal = normalize(input.normalWS);
                half3 lighting = max(SampleSH(normal), half3(0,0,0)) + sun.color * saturate(dot(normal, sun.direction)) * sun.shadowAttenuation * .75;
                half3 color = surface.rgb * _Tint.rgb * lerp(half3(1,1,1), input.color.rgb, _UseVertexColor) * lighting;
                return half4(MixFog(color, input.fog), 1);
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
            #pragma multi_compile_instancing
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
