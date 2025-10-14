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
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType is CameraType.Game or CameraType.SceneView)
            {
                var settings = VolumeManager.instance.stack.GetComponent<HeightFogSettings>();
                // if (settings.Enable.value)
                {
                    _pass.Setup(settings);
                    renderer.EnqueuePass(_pass);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            BlitUtils.CleanUp();
        }
    }
}
