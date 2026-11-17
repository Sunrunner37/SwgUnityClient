Shader "SWG/World"
{
    Properties
    {
        [HideInInspector] _SwgInteriorLighting("Native room lighting", Float) = 0
        [HideInInspector] _SwgExteriorPortalClip("Exterior visible through entrances", Float) = 0
        _SwgPreview("UI preview lighting", Float) = 0
        _BaseMap("Diffuse", 2D) = "white" {}
        _Fullbright("Native full emissive", Float) = 0
        _EmissionColor("Native material emission", Color) = (0,0,0,0)
        [HideInInspector] _SrcBlend("Source blend", Float) = 1
        [HideInInspector] _DstBlend("Destination blend", Float) = 0
        [HideInInspector] _ZWrite("Depth write", Float) = 1
        [HideInInspector] _AlphaBlend("Alpha blend", Float) = 0
        [HideInInspector] _AlphaToMask("Alpha to coverage", Float) = 1
        [HideInInspector] _TerrainHoleMap("Interior footprints", 2D) = "black" {}
        [HideInInspector] _TerrainHoleRect("Interior footprint coordinates", Vector) = (0,0,0,0)
        _TerrainBlend("Terrain layer blending", Float) = 0
        _TerrainMap1("Terrain layer 1", 2D) = "white" {}
        _TerrainMap2("Terrain layer 2", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _FloraTint("Flora terrain tint", Color) = (1,1,1,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = 0
        _UseVertexColor("Use vertex color", Float) = 0
        [HideInInspector] _VertexColorAmbientOnly("Vertex color affects ambient only", Float) = 0
        _Billboard("Billboard", Float) = 0
        _SwayDisplacement("Sway displacement", Float) = 0
        _SwayPeriod("Sway period", Float) = 1
        _SwayHeight("Sway height", Float) = 1
        _FloraMinimumDistance("Flora minimum distance", Float) = 0
        _FloraMaximumDistance("Flora maximum distance", Float) = 0
        _FloraFadeDistance("Flora fade band", Float) = 24
        _FloraShadowDistance("Flora shadow distance", Float) = 0
        _FloraWidthScale("Distant grass card width", Float) = 1
        _FloraWidthStart("Grass width transition start", Float) = 40
        _FloraWidthEnd("Grass width transition end", Float) = 160
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
            #pragma target 4.5
            #define _ALPHATEST_ON 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SwgShadows.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; float3 blend:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; float3 worldPosition:TEXCOORD3; half3 blend:TEXCOORD4; float2 terrainUv:TEXCOORD5; half3 floraTint:TEXCOORD6; float2 floraSeed:TEXCOORD7; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_TerrainHoleMap); SAMPLER(sampler_TerrainHoleMap);
            TEXTURE2D(_TerrainMap1); SAMPLER(sampler_TerrainMap1);
            TEXTURE2D(_TerrainMap2); SAMPLER(sampler_TerrainMap2);
            float4 _SwgFloraCamera;
            #if defined(SWG_EXTERIOR_PORTALS)
            StructuredBuffer<float4> _SwgExteriorPortals;
            #endif
            CBUFFER_START(UnityPerMaterial)
            float _SwgInteriorLighting;
            float _SwgExteriorPortalClip;
            int _SwgExteriorPortalCount;
            half4 _SwgInteriorAmbient;
            int _SwgInteriorLightCount;
            float4 _SwgInteriorLightPositions[16];
            float4 _SwgInteriorLightColors[16];
            float4 _SwgInteriorLightAttenuation[16];
            float4 _SwgInteriorLightDirections[16];
            float4 _BaseMap_ST;
            float4 _TerrainMap1_ST;
            float4 _TerrainMap2_ST;
            float _TerrainBlend;
            float4 _TerrainHoleRect;
            float _Fullbright;
            half4 _EmissionColor;
            float _AlphaBlend;
            half4 _Tint;
            half _Cutoff;
            float _SwgPreview;
            half _UseVertexColor;
            half _VertexColorAmbientOnly;
            float _Billboard;
            float _SwayDisplacement;
            float _SwayPeriod;
            float _SwayHeight;
            float _FloraMinimumDistance;
            float _FloraMaximumDistance;
            float _FloraFadeDistance;
            float _FloraShadowDistance;
            float _FloraWidthScale;
            float _FloraWidthStart;
            float _FloraWidthEnd;
            CBUFFER_END
            // A camera-to-fragment segment must cross a native exterior portal. Merely
            // projecting a doorway to the screen would also reveal shell walls in front of it.
            void ClipExterior(float3 positionWS)
            {
                #if defined(SWG_EXTERIOR_PORTALS)
                if (_SwgExteriorPortalClip < .5) return;
                float3 origin = GetCameraPositionWS();
                float3 ray = positionWS - origin;
                for (int i = 0; i < _SwgExteriorPortalCount; i++)
                {
                    float3 a = _SwgExteriorPortals[i * 3].xyz;
                    float3 edge1 = _SwgExteriorPortals[i * 3 + 1].xyz - a;
                    float3 edge2 = _SwgExteriorPortals[i * 3 + 2].xyz - a;
                    float3 p = cross(ray, edge2);
                    float determinant = dot(edge1, p);
                    if (abs(determinant) < 1e-6) continue;
                    float3 offset = origin - a;
                    float u = dot(offset, p) / determinant;
                    float3 q = cross(offset, edge1);
                    float v = dot(ray, q) / determinant;
                    float t = dot(edge2, q) / determinant;
                    if (u >= -1e-4 && v >= -1e-4 && u + v <= 1.0001 && t >= 0 && t <= 1.0001) return;
                }
                clip(-1);
                #endif
            }


            UNITY_INSTANCING_BUFFER_START(Flora)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FloraTint)
            UNITY_INSTANCING_BUFFER_END(Flora)
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 position = input.positionOS.xyz;
                float3 center = TransformObjectToWorld(float3(0,0,0));
                if (_FloraMaximumDistance > 0)
                    position.x *= lerp(1, _FloraWidthScale, smoothstep(_FloraWidthStart, _FloraWidthEnd, distance(center, _SwgFloraCamera.xyz)));
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
                output.floraSeed = frac(center.xz * float2(.754877, .569840)) * 127;
                output.positionCS = TransformWorldToHClip(worldPosition);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.floraTint = UNITY_ACCESS_INSTANCED_PROP(Flora, _FloraTint).rgb;
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.color = input.color;
                output.blend = input.blend;
                output.terrainUv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half SurfaceCoverage(half alpha, float3 worldPosition)
            {
                if (_Cutoff <= 0)
                    return 1;
                if (_FloraMaximumDistance > 0 && IsAlphaToMaskAvailable())
                {
                    // Resolve mip coverage through MSAA samples instead of random whole-pixel holes.
                    float filtered = smoothstep(_FloraWidthStart, _FloraWidthEnd, distance(_SwgFloraCamera.xyz, worldPosition));
                    half coverage = lerp(SharpenAlphaStrict(alpha, _Cutoff), alpha, filtered);
                    clip(coverage - .0001);
                    return coverage;
                }
                half coverage = AlphaClip(alpha, _Cutoff);
                return IsAlphaToMaskAvailable() ? coverage : 1;
            }
            void ClipFloraFade(float3 worldPosition, float2 pixel, float2 seed, bool shadow)
            {
                if (_FloraMaximumDistance <= 0)
                    return;
                float distanceToCamera = distance(_SwgFloraCamera.xyz, worldPosition);
                float fade = saturate((_FloraMaximumDistance - distanceToCamera) / max(.001, _FloraFadeDistance));
                if (_FloraMinimumDistance > 0)
                    fade *= saturate((distanceToCamera - _FloraMinimumDistance) / max(.001, _FloraFadeDistance));
                if (shadow && _FloraShadowDistance > 0)
                    fade *= saturate((_FloraShadowDistance - distanceToCamera) / max(.001, _FloraFadeDistance));
                // Only the actual distance-transition bands use screen-door fading.
                if (fade < 1)
                    clip(fade - max(.0001, InterleavedGradientNoise(pixel + seed, 0)));
            }
            void ClipInteriorTerrain(float3 worldPosition)
            {
                if (_TerrainBlend > .5 && _TerrainHoleRect.z > 0)
                    clip(.5 - SAMPLE_TEXTURE2D(_TerrainHoleMap, sampler_TerrainHoleMap, (worldPosition.xz - _TerrainHoleRect.xy) * _TerrainHoleRect.zw).r);
            }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            AlphaToMask [_AlphaToMask]
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ SWG_EXTERIOR_PORTALS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            half4 Frag(Varyings input):SV_Target
            {
                ClipInteriorTerrain(input.worldPosition);
                ClipExterior(input.worldPosition);
                half4 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                if (_TerrainBlend > .5)
                {
                    half4 layer1 = SAMPLE_TEXTURE2D(_TerrainMap1, sampler_TerrainMap1, input.terrainUv * _TerrainMap1_ST.xy + _TerrainMap1_ST.zw);
                    half4 layer2 = SAMPLE_TEXTURE2D(_TerrainMap2, sampler_TerrainMap2, input.terrainUv * _TerrainMap2_ST.xy + _TerrainMap2_ST.zw);
                    surface = surface * input.blend.x + layer1 * input.blend.y + layer2 * input.blend.z;
                }
                if (_AlphaBlend > .5) clip(surface.a - _Cutoff);
                half coverage = _AlphaBlend > .5 ? surface.a : SurfaceCoverage(surface.a, input.worldPosition);
                ClipFloraFade(input.worldPosition, input.positionCS.xy, input.floraSeed, false);
                half3 normal = normalize(input.normalWS);
                half3 vertexTint = lerp(half3(1,1,1), input.color.rgb, _UseVertexColor);
                if (_SwgInteriorLighting > .5) vertexTint = SRGBToLinear(vertexTint);
                half3 ambientTint = lerp(half3(1,1,1), vertexTint, _VertexColorAmbientOnly);
                half3 lighting = SwgSurfaceLighting(normal, input.worldPosition, _SwgInteriorLighting, _SwgInteriorAmbient.rgb, ambientTint);
                if (_SwgInteriorLighting > .5)
                    lighting += SwgInteriorDirectLighting(normal, input.worldPosition, _SwgInteriorLightCount, _SwgInteriorLightPositions, _SwgInteriorLightColors, _SwgInteriorLightAttenuation, _SwgInteriorLightDirections);
                half previewBrightness = .55 + .45 * saturate(dot(normal, normalize(float3(-.35,.6,.8))));
                lighting = lerp(lighting, previewBrightness.xxx, _SwgPreview);
                lighting = lerp(lighting, half3(1,1,1), _Fullbright);
                half3 color = surface.rgb * _Tint.rgb * input.floraTint * lerp(vertexTint, half3(1,1,1), _VertexColorAmbientOnly) * lighting;
                color += surface.rgb * _EmissionColor.rgb * (1 - _Fullbright);
                return half4(lerp(MixFog(color, input.fog), color, _SwgPreview), coverage);
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
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPosition : TEXCOORD1; float2 floraSeed : TEXCOORD2; };
            ShadowVaryings ShadowVert(Attributes input)
            {
                Varyings vertex = Vert(input);
                ShadowVaryings output;
                output.positionCS = SwgShadowPosition(vertex.worldPosition, vertex.normalWS);
                output.uv = vertex.uv;
                output.worldPosition = vertex.worldPosition;
                output.floraSeed = vertex.floraSeed;
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                ClipInteriorTerrain(input.worldPosition);
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                ClipFloraFade(input.worldPosition, input.positionCS.xy, input.floraSeed, true);
                return 0;
            }
            ENDHLSL
        }
        // Drawn explicitly into terrain-tile masks, never as a camera-visible pass.
        Pass
        {
            Name "TerrainCutout"
            Tags { "LightMode"="TerrainCutout" }
            ZTest Always
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex CutoutVert
            #pragma fragment CutoutFrag
            float4 CutoutVert(Attributes input) : SV_POSITION
            {
                float3 position = TransformObjectToWorld(input.positionOS.xyz);
                float2 uv = (position.xz - _TerrainHoleRect.xy) * _TerrainHoleRect.zw;
                float4 clipPosition = float4(uv * 2 - 1, 0, 1);
                #if UNITY_UV_STARTS_AT_TOP
                    clipPosition.y = -clipPosition.y;
                #endif
                return clipPosition;
            }
            half4 CutoutFrag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}
