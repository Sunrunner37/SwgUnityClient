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
#endif
