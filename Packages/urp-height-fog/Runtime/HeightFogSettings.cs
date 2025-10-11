using UnityEngine.Rendering;

namespace HeightFog.Runtime
{
    [VolumeComponentMenu("Atmospheric Effects/Height Fog")]
    public class HeightFogSettings : VolumeComponent
    {
        public BoolParameter Enable = new(false);

        public ColorParameter Color = new(UnityEngine.Color.white);
        public ClampedFloatParameter Density = new(0.1f, 0.0f, 1.0f);
        public MinFloatParameter Distance = new(25.0f, 0.001f);
        public FloatParameter Height = new(1.0f);
        public ClampedFloatParameter HeightIntensity = new(0.2f, 0.0f, 1.0f);
    }
}
