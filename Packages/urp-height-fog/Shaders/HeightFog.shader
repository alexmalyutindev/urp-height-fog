Shader "Hidden/HeightFog"
{
    Properties {}
    HLSLINCLUDE
    #pragma target 3.5
    #pragma prefer_hlslcc gles metal

    #pragma vertex Vertex
    #pragma fragment Fragment

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
    // #define HEIGHT_FOG_EXP2
    // #define HEIGHT_FOG_HYP2
    #include "Packages/com.alexmalyutindev.urp-heigh-fog/Shaders/HeightFog.hlsl"

    // Z buffer to linear view space (eye) depth.
    // Does NOT correctly handle oblique view frustums.
    // Does NOT work with orthographic projection.
    // zBufferParam (UNITY_REVERSED_Z) = { f/n - 1,   1, (1/n - 1/f), 1/f }
    // zBufferParam                    = { 1 - f/n, f/n, (1/f - 1/n), 1/n }
    half LinearEyeDepth_half(half depth, half4 zBufferParam)
    {
        return rcp(zBufferParam.z * depth + zBufferParam.w);
    }

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        half4 positionCS : SV_POSITION;
        half3 viewDirectionWS : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        half4 positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        // NOTE: Optimized version of:
        // float4 positionVS = mul(unity_MatrixInvP, float4(positionCS.xy, half(1.0h), half(1.0h)));
        // positionVS /= positionVS.w;
        // positionVS.xyz /= abs(positionVS.z);
        half3 positionVS = half3(positionCS.xy * unity_MatrixInvP._m00_m11, -1.0h);

        output.positionCS = positionCS;
        output.viewDirectionWS = mul((half3x3)unity_MatrixInvV, positionVS.xyz);

        return output;
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "HeightFog OneShot ManualBlend"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            half4 Fragment(Varyings input) : SV_Target
            {
                half density, distance, planeY, heightIntensity;
                GetFogParams(density, distance, planeY, heightIntensity);

                half sceneDepth = LoadSceneDepth(input.positionCS.xy);
                sceneDepth = LinearEyeDepth_half(sceneDepth, half4(_ZBufferParams));
                half3 sceneColor = LoadSceneColor(input.positionCS.xy);

                half viewDirectionLength = length(input.viewDirectionWS);
                half viewDirectionWS_normY = input.viewDirectionWS.y * rcp(viewDirectionLength);

                half cameraPositionWS_Y = half(GetCameraPositionWS().y);
                half realSceneDepth = viewDirectionLength * sceneDepth;
                half fogThickness = min(realSceneDepth, distance);
                half positionWS_Y = cameraPositionWS_Y + viewDirectionWS_normY * fogThickness;

                if (cameraPositionWS_Y > planeY && positionWS_Y > planeY)
                {
                    return half4(sceneColor, 1.0h);
                }

                half fogFactor = ComputeFogDensity(fogThickness * density);
                half heightFactor = smoothstep(0.0h, 1.0h, (planeY - positionWS_Y) * heightIntensity);

                return half4(lerp(sceneColor, half3(_FogColor.rgb), fogFactor * heightFactor), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HeightFog OneShot AlphaBlend"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            half4 Fragment(Varyings input) : SV_Target
            {
                half density, distance, planeY, heightIntensity;
                GetFogParams(density, distance, planeY, heightIntensity);

                half sceneDepth = LoadSceneDepth(input.positionCS.xy);
                sceneDepth = LinearEyeDepth_half(sceneDepth, half4(_ZBufferParams));

                half viewDirectionLength = length(input.viewDirectionWS);
                half viewDirectionWS_normY = input.viewDirectionWS.y * rcp(viewDirectionLength);

                half cameraPositionWS_Y = GetCameraPositionWS().y;
                half realSceneDepth = viewDirectionLength * sceneDepth;
                half fogThickness = min(realSceneDepth, distance);
                half positionWS_Y = cameraPositionWS_Y + viewDirectionWS_normY * fogThickness;

                if (cameraPositionWS_Y > planeY && positionWS_Y > planeY)
                {
                    discard;
                }

                half fogFactor = ComputeFogDensity(fogThickness * density);
                half heightFactor = smoothstep(0.0h, 1.0h, (planeY - positionWS_Y) * heightIntensity);

                return half4(_FogColor.rgb, fogFactor * heightFactor);
            }
            ENDHLSL
        }
    }
}