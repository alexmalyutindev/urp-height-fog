// цвет
float4 _FogColor;
// Density, Distance, Height, HeightIntensity.
float4 _FogParams;

#define _FogDensity (_FogParams.x)
#define _FogDistance (_FogParams.y)
#define _FogPlaneY (_FogParams.z)
#define _FogHeightIntensity (_FogParams.w)

// #define HEIGHT_FOG_EXP
#define HEIGHT_FOG_HYP
#define HEIGHT_FOG_DISTFADE_HIGH

half4 ComputeHeightFog(float3 positionWS)
{
    float3 cameraPositionWS = GetCameraPositionWS();

    half fogThickness = distance(cameraPositionWS, positionWS);
    half heightFactor = saturate((_FogPlaneY - positionWS.y) * _FogHeightIntensity);

#if defined(HEIGHT_FOG_DISTFADE_HIGH)
    float3 viewDirectionWS = positionWS - cameraPositionWS;
    float planeDistance = fogThickness / viewDirectionWS.y * min(0.0f, _FogPlaneY - cameraPositionWS.y);
    half distanceFadeFactor = smoothstep(_FogDistance, _FogDistance * 0.75h, planeDistance);
#else // HEIGHT_FOG_DISTFADE_LOW
    fogThickness = min(fogThickness, _FogDistance);
    half distanceFadeFactor = smoothstep(_FogDistance * 1.2f, _FogDistance * 0.8f, fogThickness);
#endif

#if defined(HEIGHT_FOG_EXP)
    half fogFactor = 1.0h - exp(-fogThickness * _FogDensity);
#elif defined(HEIGHT_FOG_HYP) 
    half fogFactor = 1.0h - 1.0h / (1.0h + fogThickness * _FogDensity);
#endif

    return half4(_FogColor.rgb, fogFactor * heightFactor * distanceFadeFactor);
}