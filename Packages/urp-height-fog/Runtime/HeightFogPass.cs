using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HeightFog.Runtime
{
    public class HeightFogPass : ScriptableRenderPass
    {
        private const int FogManualBlendPasId = 0;
        private const int FogAlphaBlendPassId = 1;

        private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
        private static readonly int FogParamsId = Shader.PropertyToID("_FogParams");

        private readonly bool _isMaterialPresented;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _props;

        private Color _fogColor;
        private Vector4 _fogParams;
        private readonly bool _useManualBlend;

        public HeightFogPass(Material material)
        {
            _props = new MaterialPropertyBlock();

            _material = material;
            _isMaterialPresented = _material != null;
            profilingSampler = ProfilingSampler.Get(CustomRenderFeature.HeightFogPas);

            
            _useManualBlend = IsGPUPreferManualBlend();

            var requiredInput = ScriptableRenderPassInput.Depth;
            if (_useManualBlend)
            {
                requiredInput |= ScriptableRenderPassInput.Color;
            }

            // NOTE: Two or One fullscreen buffer copy of _CameraDepthTexture and _CameraOpaqueTexture made by URP,
            // depending on _useManualBlend or not!
            ConfigureInput(requiredInput);
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

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                _props.SetColor(FogColorId, _fogColor);
                _props.SetVector(FogParamsId, _fogParams);

                var pass = _useManualBlend ? FogManualBlendPasId : FogAlphaBlendPassId;
                BlitUtils.DrawTriangle(cmd, _material, pass, _props);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        private static bool IsGPUPreferManualBlend()
        {
            string gpuName = SystemInfo.graphicsDeviceName;
            // TODO: Collect a table of GPUs, that prefer manual blend instead of AlphaBlend!
            bool preferManualBlend = gpuName.Contains("Mali");
            return preferManualBlend;
        }
    }
}
