Shader "Hidden/HeightFog"
{
    Properties {}
    SubShader
    {
        Pass
        {
            Name "HeightFog OneShot"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.alexmalyutindev.urp-heigh-fog/Shaders/HeightFog.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

                float4 positionNDC = float4(positionCS.xy, half(0.0), half(1.0));
                float4 positionVS = mul(unity_MatrixInvP, positionNDC);
                positionVS /= positionVS.w;
                positionVS.xyz /= abs(positionVS.z);

                output.positionCS = positionCS;
                output.texcoord = uv;
                output.viewDirectionWS = mul(unity_MatrixInvV, half4(positionVS.xyz, 0.0f)).xyz;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = SampleSceneDepth(input.texcoord);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                half3 positionWS = GetCameraPositionWS() + input.viewDirectionWS * sceneDepth;
                return ComputeHeightFog(positionWS);
            }
            ENDHLSL
        }
    }
}