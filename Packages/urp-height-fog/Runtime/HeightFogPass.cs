using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
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
        private bool _useAlphaBlend;

        public HeightFogPass(Material material)
        {
            _props = new MaterialPropertyBlock();

            _material = material;
            _isMaterialPresented = _material != null;
            profilingSampler = ProfilingSampler.Get(CustomRenderFeature.HeightFogPas);

            // NOTE: Only two fullscreen buffer copy of _CameraDepthTexture and _CameraOpaqueTexture made by URP.
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public void Setup(HeightFogSettings settings, bool useAlphaBlend)
        {
            _fogColor = settings.Color.value;
            _fogParams = new Vector4(
                settings.Density.value,
                settings.Distance.value,
                settings.Height.value,
                settings.HeightIntensity.value
            );
            _useAlphaBlend = useAlphaBlend;
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

                var pass = _useAlphaBlend ? 1 : 0;
                BlitUtils.DrawTriangle(cmd, _material, pass, _props);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
