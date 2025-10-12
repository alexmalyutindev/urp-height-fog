using UnityEngine;
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

        public HeightFogPass(Material material)
        {
            _props = new MaterialPropertyBlock();

            _material = material;
            _isMaterialPresented = _material != null;

            // TODO: Для данных глубины используйте возможности URP/камеры. Все решения фиксируются в результате.
            ConfigureInput(ScriptableRenderPassInput.Depth);
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

            var fogParams = new Vector4(
                settings.Density.value,
                settings.Distance.value,
                settings.Height.value,
                settings.HeightIntensity.value
            );
            _props.SetVector(FogParamsId, fogParams);
            _props.SetColor(FogColorId, settings.Color.value);

            BlitUtils.DrawTriangle(cmd, _material, 0, _props);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
