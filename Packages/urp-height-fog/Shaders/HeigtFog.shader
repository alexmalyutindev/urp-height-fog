Shader "Hidden/HeightFog"
{
    Properties {}

    HLSLINCLUDE
    #pragma target 3.5

    #pragma vertex Vertex
    #pragma fragment Fragment

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
    #include "Packages/com.alexmalyutindev.urp-heigh-fog/Shaders/HeightFog.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        half3 viewDirectionWS : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        float4 positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
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
            Name "HeightFog OneShot"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            // #define HEIGHT_FOG_EXP2
            inline half ComputeFogDensity(half thickness)
            {
                #if defined(HEIGHT_FOG_EXP2)
                return 1.0h - exp2(-thickness);
                #else
                thickness = mad(thickness, 0.5h, 1.0h);
                return 1.0h - rcp(thickness * thickness);
                #endif
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = LoadSceneDepth(input.positionCS.xy);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                half3 sceneColor = LoadSceneColor(input.positionCS.xy);

                half viewDirectionLengthRcp = rcp(length(input.viewDirectionWS));
                half viewDirectionWS_normY = input.viewDirectionWS.y * viewDirectionLengthRcp;

                half cameraPositionWS_Y = GetCameraPositionWS().y;
                half realSceneDepth = length(input.viewDirectionWS) * sceneDepth;

                half fogThickness = min(realSceneDepth, _FogDistance);
                half positionWS_Y = cameraPositionWS_Y + viewDirectionWS_normY * min(realSceneDepth, _FogDistance);

                half fogFactor = ComputeFogDensity(fogThickness * _FogDensity);
                half heightFactor = smoothstep(0.0h, 1.0h, (_FogPlaneY - positionWS_Y) * _FogHeightIntensity);
                return half4(lerp(sceneColor, _FogColor.rgb, fogFactor * heightFactor), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HeightFog Intermediate 1/2"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = LoadSceneDepth(input.positionCS.xy * 2.0f);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                half3 positionWS = GetCameraPositionWS() + input.viewDirectionWS * sceneDepth;
                return ComputeHeightFog(positionWS).a;
            }
            ENDHLSL
        }
        // TODO: Blit pass!!
    }
}