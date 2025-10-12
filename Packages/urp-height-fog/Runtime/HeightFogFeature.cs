using UnityEngine;
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

            // TODO: Выбор точки вставки в пайплайн самостоятельный. При отправке результата нужно зафиксировать выбранное место и причину выбора.
            _pass = new HeightFogPass(HeightFogMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingSkybox,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType is CameraType.Game or CameraType.SceneView)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            BlitUtils.CleanUp();
        }
    }
}
