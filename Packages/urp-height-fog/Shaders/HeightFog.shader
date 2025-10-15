Shader "Hidden/HeightFog"
{
    Properties {}
    SubShader
    {
        Pass
        {
            Name "HeightFog OneShot ManualBlend"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            // #define HEIGHT_FOG_EXP2
            // #define HEIGHT_FOG_HYP2
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

            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = LoadSceneDepth(input.positionCS.xy);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                half3 sceneColor = LoadSceneColor(input.positionCS.xy);

                half viewDirectionLength = length(input.viewDirectionWS);
                half viewDirectionWS_normY = input.viewDirectionWS.y * rcp(viewDirectionLength);

                half cameraPositionWS_Y = GetCameraPositionWS().y;
                half realSceneDepth = viewDirectionLength * sceneDepth;
                half fogThickness = min(realSceneDepth, _FogDistance);
                half positionWS_Y = cameraPositionWS_Y + viewDirectionWS_normY * fogThickness;

                if (cameraPositionWS_Y > _FogPlaneY && positionWS_Y > _FogPlaneY)
                {
                    return half4(sceneColor, 1.0h);
                }

                half fogFactor = ComputeFogDensity(fogThickness * _FogDensity);
                half heightFactor = smoothstep(0.0h, 1.0h, (_FogPlaneY - positionWS_Y) * _FogHeightIntensity);

                return half4(lerp(sceneColor, _FogColor.rgb, fogFactor * heightFactor), 1.0h);
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
            #pragma target 3.5

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            // #define HEIGHT_FOG_EXP2
            // #define HEIGHT_FOG_HYP2
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

            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = LoadSceneDepth(input.positionCS.xy);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);

                half viewDirectionLength = length(input.viewDirectionWS);
                half viewDirectionWS_normY = input.viewDirectionWS.y * rcp(viewDirectionLength);

                half cameraPositionWS_Y = GetCameraPositionWS().y;
                half realSceneDepth = viewDirectionLength * sceneDepth;
                half fogThickness = min(realSceneDepth, _FogDistance);
                half positionWS_Y = cameraPositionWS_Y + viewDirectionWS_normY * fogThickness;

                if (cameraPositionWS_Y > _FogPlaneY && positionWS_Y > _FogPlaneY)
                {
                    clip(-1.0f);
                    return 0.0h;
                }

                half fogFactor = ComputeFogDensity(fogThickness * _FogDensity);
                half heightFactor = smoothstep(0.0h, 1.0h, (_FogPlaneY - positionWS_Y) * _FogHeightIntensity);

                return half4(_FogColor.rgb, fogFactor * heightFactor);
            }
            ENDHLSL
        }
    }
}