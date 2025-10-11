using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HeightFog.Runtime
{
    public class HeightFogFeature : ScriptableRendererFeature
    {
        public Material HeightFogMaterial;
        private HeightFogPass _pass;

        public override void Create()
        {
            BlitUtils.Initialize();

            _pass = new HeightFogPass(HeightFogMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingSkybox,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            BlitUtils.CleanUp();
        }
    }

    public class HeightFogPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private readonly MaterialPropertyBlock _props;

        public HeightFogPass(Material material)
        {
            _material = material;
            _props = new MaterialPropertyBlock();
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
            
            if (!settings.Enable.value) return;

            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                _props.SetColor("_FogColor", settings.Color.value);

                var fogParams = new Vector4(
                    settings.Density.value,
                    settings.Distance.value,
                    settings.Height.value,
                    settings.HeightIntensity.value
                );
                _props.SetVector("_FogParams", fogParams);

                BlitUtils.DrawTriangle(cmd, _material, 0, _props);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
