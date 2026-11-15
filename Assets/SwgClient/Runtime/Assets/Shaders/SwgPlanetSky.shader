Shader "SWG/PlanetSky"
{
    Properties
    {
        _Gradient("Native sky gradient", 2D) = "white" {}
        _HasGradient("Has gradient", Float) = 0
        _DayPhase("Time of day", Range(0,1)) = .25
        _Fallback("Clear color", Color) = (0,0,0,1)
        _BottomMap("Bottom clouds", 2D) = "white" {}
        _BottomNormal("Bottom cloud normals", 2D) = "bump" {}
        _TopMap("Top clouds", 2D) = "white" {}
        _TopNormal("Top cloud normals", 2D) = "bump" {}
        _BottomEnabled("Bottom enabled", Float) = 0
        _TopEnabled("Top enabled", Float) = 0
        _BottomMotion("Bottom size/speed", Vector) = (45,.01,0,0)
        _TopMotion("Top size/speed", Vector) = (90,.01,0,0)
        _CloudAmbient("Ambient", Color) = (1,1,1,1)
        _CloudSun("Sun", Color) = (0,0,0,1)
        _SunDirection("Sun direction", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            TEXTURE2D(_Gradient); SAMPLER(sampler_Gradient);
            TEXTURE2D(_BottomMap); SAMPLER(sampler_BottomMap);
            TEXTURE2D(_BottomNormal); SAMPLER(sampler_BottomNormal);
            TEXTURE2D(_TopMap); SAMPLER(sampler_TopMap);
            TEXTURE2D(_TopNormal); SAMPLER(sampler_TopNormal);
            CBUFFER_START(UnityPerMaterial)
                float _HasGradient, _DayPhase, _BottomEnabled, _TopEnabled;
                float4 _Fallback, _BottomMotion, _TopMotion, _CloudAmbient, _CloudSun, _SunDirection;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.direction = input.positionOS.xyz;
                return o;
            }
            half4 Cloud(float2 plane, TEXTURE2D_PARAM(map, mapSampler), TEXTURE2D_PARAM(normals, normalSampler), float4 motion)
            {
                float2 uv = plane / (motion.x != 0 ? motion.x : 1) + float2(_Time.y * motion.y, 0);
                uv.y = 1 - uv.y;
                half4 cloud = SAMPLE_TEXTURE2D(map, mapSampler, uv);
                half3 n = SAMPLE_TEXTURE2D(normals, normalSampler, uv).xyz * 2 - 1;
                cloud.rgb *= _CloudAmbient.rgb + _CloudSun.rgb * saturate(dot(normalize(n.xzy), _SunDirection.xyz));
                return cloud;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 ray = normalize(input.direction);
                // Native sky geometry halves its Y coordinate before projection.
                float height = saturate(normalize(ray * float3(1,2,1)).y);
                half3 color = _Fallback.rgb;
                if (_HasGradient > .5) color = SAMPLE_TEXTURE2D(_Gradient, sampler_Gradient, float2(_DayPhase, height)).rgb;
                if (ray.y > .0001 && (_TopEnabled > .5 || _BottomEnabled > .5))
                {
                    // Native clouds are a camera-relative grid at Y=10, with 50m border feathering.
                    float2 plane = ray.xz * (10 / ray.y);
                    float fade = saturate(min(min(plane.x + 250, 200 - plane.x), min(plane.y + 250, 200 - plane.y)) / 50);
                    if (fade > 0)
                    {
                        if (_TopEnabled > .5) { half4 c = Cloud(plane, TEXTURE2D_ARGS(_TopMap,sampler_TopMap), TEXTURE2D_ARGS(_TopNormal,sampler_TopNormal), _TopMotion); color = lerp(color,c.rgb,c.a*fade); }
                        if (_BottomEnabled > .5) { half4 c = Cloud(plane, TEXTURE2D_ARGS(_BottomMap,sampler_BottomMap), TEXTURE2D_ARGS(_BottomNormal,sampler_BottomNormal), _BottomMotion); color = lerp(color,c.rgb,c.a*fade); }
                    }
                }
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
