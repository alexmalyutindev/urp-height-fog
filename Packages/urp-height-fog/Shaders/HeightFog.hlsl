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

inline half ComputeFogDensity(half thickness)
{
    #if defined(HEIGHT_FOG_EXP2)
    return 1.0h - exp2(-thickness);
    #elif defined(HEIGHT_FOG_HYP2)
    thickness = mad(thickness, 0.5h, 1.0h);
    return 1.0h - rcp(thickness * thickness);
    #else
    return 1.0h - rcp(1.0h + thickness);
    #endif
}

#endif
