#ifndef HEIGHT_FOG_INCLUDED
#define HEIGHT_FOG_INCLUDED

half4 _FogColor;
// Density, Distance, Height, HeightIntensity.
half4 _FogParams;

#define _FogDensity (_FogParams.x)
#define _FogDistance (_FogParams.y)
#define _FogPlaneY (_FogParams.z)
#define _FogHeightIntensity (_FogParams.w)

// #define HEIGHT_FOG_EXP
#define HEIGHT_FOG_HYP
#define HEIGHT_FOG_DISTFADE_RAY_CORRECTION

half4 ComputeHeightFog(float3 positionWS)
{
    float3 cameraPositionWS = GetCameraPositionWS();
    if (cameraPositionWS.y > _FogPlaneY && positionWS.y > cameraPositionWS.y)
    {
        // NOTE: Early exit, if camera above fog plane and view ray not intersection it.
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    half fogThickness = distance(cameraPositionWS, positionWS);

#if defined(HEIGHT_FOG_DISTFADE_RAY_CORRECTION)
    // NOTE: Modifying end of ray, to apply correct height fade. At the same time it will add distance fade!
    half clampedFogThickness = min(fogThickness, _FogDistance);
    positionWS = cameraPositionWS + (positionWS - cameraPositionWS) * rcp(fogThickness) * clampedFogThickness;
    fogThickness = clampedFogThickness;
    const half distanceFadeFactor = 1.0h;
#elif defined(HEIGHT_FOG_DISTFADE_PLANE_INTESECTION)
    // NOTE: Calculate ray-plane intersection and distanceFadeFactor that based on distance to this point.  
    float3 viewDirectionWS = positionWS - cameraPositionWS;
    float planeDistance = fogThickness / viewDirectionWS.y * min(0.0f, _FogPlaneY - cameraPositionWS.y);
    half distanceFadeFactor = smoothstep(_FogDistance, _FogDistance * 0.75h, planeDistance);
#else // HEIGHT_FOG_DISTFADE_SIMPLE
    fogThickness = min(fogThickness, _FogDistance);
    half distanceFadeFactor = smoothstep(_FogDistance * 1.2f, _FogDistance * 0.8f, fogThickness);
#endif

    // NOTE: Computing global height factor, looks better and easier to control. 
    half heightFactor = saturate((_FogPlaneY - positionWS.y) * _FogHeightIntensity);

#if defined(HEIGHT_FOG_EXP)
    half fogFactor = 1.0h - exp2(-fogThickness * _FogDensity);
#elif defined(HEIGHT_FOG_HYP) 
    half fogFactor = 1.0h - rcp(1.0h + fogThickness * _FogDensity);
#endif

    return half4(_FogColor.rgb, fogFactor * heightFactor * distanceFadeFactor);
}

#endif
