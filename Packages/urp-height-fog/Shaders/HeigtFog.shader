Shader "Hidden/HeightFog"
{
    Properties {}
    SubShader
    {
        Pass
        {
            Name "HeightFog OneShot"
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

                output.positionCS = positionCS;
                output.texcoord = uv;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half sceneDepth = SampleSceneDepth(input.texcoord);
                sceneDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                sceneDepth = min(sceneDepth, 100);
                return 1.0h - exp(-sceneDepth * 0.1h);
            }
            ENDHLSL
        }
    }
}