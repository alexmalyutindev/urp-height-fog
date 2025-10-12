using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HeightFog.Runtime
{
    public class HeightFogPass : ScriptableRenderPass
    {
        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        private static readonly int FogParamsId = Shader.PropertyToID("_FogParams");

        private readonly bool _isMaterialPresented;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _props;

        private bool _useIntermediateBuffer;
        private RTHandle _intermediateBuffer;
        private RTHandle _maxDepthBuffer;
        private Color _fogColor;
        private Vector4 _fogParams;

        public HeightFogPass(Material material)
        {
            _props = new MaterialPropertyBlock();

            _material = material;
            _isMaterialPresented = _material != null;

            // TODO: Для данных глубины используйте возможности URP/камеры. Все решения фиксируются в результате.
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public void Setup(HeightFogSettings settings)
        {
            _fogColor = settings.Color.value;
            _fogParams = new Vector4(
                settings.Density.value,
                settings.Distance.value,
                settings.Height.value,
                settings.HeightIntensity.value
            );
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (_useIntermediateBuffer)
            {
                var width = cameraTextureDescriptor.width / 2;
                var height = cameraTextureDescriptor.height / 2;
                var desc = new RenderTextureDescriptor(width, height
                )
                {
                    graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.R8, false),
                    depthBufferBits = 0,
                    msaaSamples = 1,
                };
                RenderingUtils.ReAllocateHandleIfNeeded(ref _intermediateBuffer, desc, FilterMode.Bilinear);

                desc.graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.RG32, false);
                RenderingUtils.ReAllocateHandleIfNeeded(ref _maxDepthBuffer, desc);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_isMaterialPresented)
            {
                return;
            }

            var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
            if (!settings.Enable.value)
            {
                return;
            }

            // TODO: Минимизировать количество полноэкранных копий и переключений целевых буферов. Итоговое количество необходимо указать.
            // NOTE: Only one fullscreen buffer copy of _CameraDepthTexture made by URP.
            var cmd = CommandBufferPool.Get("Height Fog");

            _props.SetColor(FogColorId, _fogColor);
            _props.SetVector(FogParamsId, _fogParams);

            if (!_useIntermediateBuffer)
            {
                BlitUtils.DrawTriangle(cmd, _material, 0, _props);
            }
            else
            {
                var renderer = renderingData.cameraData.renderer;
                // TODO: Make 1/2 DepthBuffer.
                Blitter.BlitCameraTexture(cmd, renderer.cameraDepthTargetHandle, _maxDepthBuffer);

                cmd.SetRenderTarget(_intermediateBuffer);
                BlitUtils.DrawTriangle(cmd, _material, 1, _props);
                Blitter.BlitCameraTexture(
                    cmd,
                    _intermediateBuffer,
                    renderer.cameraColorTargetHandle, 
                    bilinear:true
                );
            }


            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
