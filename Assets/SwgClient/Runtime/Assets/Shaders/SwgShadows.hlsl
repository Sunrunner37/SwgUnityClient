#ifndef SWG_SHADOWS_INCLUDED
#define SWG_SHADOWS_INCLUDED

// Supplied by URP for shadow caster rendering.
float3 _LightDirection;
float3 _LightPosition;

float4 SwgShadowPosition(float3 positionWS, float3 normalWS)
{
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 direction = normalize(_LightPosition - positionWS);
    #else
        float3 direction = _LightDirection;
    #endif
    return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction)));
}

Light SwgMainLight(float3 positionWS)
{
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoordinates = ComputeScreenPos(TransformWorldToHClip(positionWS));
    #else
        float4 shadowCoordinates = TransformWorldToShadowCoord(positionWS);
    #endif
    Light light = GetMainLight(shadowCoordinates);
    light.shadowAttenuation = lerp(light.shadowAttenuation, 1, GetMainLightShadowFade(positionWS));
    return light;
}
// Homogeneous light positions follow Unity's convention: w=0 is a direction to the light, w=1 a world position.
half3 SwgInteriorDirectLighting(half3 normalWS, float3 positionWS, int count, float4 positions[16], float4 colors[16], float4 attenuation[16], float4 directions[16])
{
    half3 result = 0;
    for (int i = 0; i < count; i++)
    {
        float3 delta = positions[i].xyz;
        float falloff = 1;
        if (positions[i].w > .5)
        {
            delta -= positionWS;
            float distanceSquared = dot(delta, delta);
            float distance = sqrt(max(distanceSquared, 1e-8));
            delta /= distance;
            falloff = max(.001, attenuation[i].x + attenuation[i].y * distance + attenuation[i].z * distanceSquared);
        }
        float emission = directions[i].w > .5 ? saturate(dot(directions[i].xyz, -delta)) : 1;
        result += colors[i].rgb * saturate(dot(normalWS, delta)) * emission / falloff;
    }
    return result;
}

half3 SwgSurfaceLighting(half3 normalWS, float3 positionWS, float interior, half3 interiorAmbient, half3 ambientTint)
{
    if (interior > .5) return interiorAmbient * ambientTint;
    Light light = SwgMainLight(positionWS);
    return max(SampleSH(normalWS), half3(0,0,0)) * ambientTint + light.color * saturate(dot(normalWS, light.direction)) * light.shadowAttenuation * .75;
}
#endif
