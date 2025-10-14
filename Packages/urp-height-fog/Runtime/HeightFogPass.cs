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

            // NOTE: Only one fullscreen buffer copy of _CameraDepthTexture and _CameraOpaqueTexture made by URP.
            var cmd = CommandBufferPool.Get("Height Fog");

            _props.SetColor(FogColorId, _fogColor);
            _props.SetVector(FogParamsId, _fogParams);

            BlitUtils.DrawTriangle(cmd, _material, 0, _props);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
