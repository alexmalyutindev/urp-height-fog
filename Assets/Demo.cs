using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using HeightFog.Runtime;

public class Demo : MonoBehaviour
{
    private const int MaxFamesCount = 1;
    private readonly FrameTiming[] _frameTimings = new FrameTiming[MaxFamesCount];

    private readonly double[] _gpuFrameTimesWithoutFog = new double[2048];
    private readonly double[] _gpuFrameTimesWithFog = new double[2048];

    public Volume Volume;

    private long _samplesCountWithFog = 0;
    private long _samplesCountWithoutFog = 0;
    private bool _enableFog = true;

    private void Start()
    {
        Application.targetFrameRate = 120;
    }

    private void Update()
    {
        if (Time.frameCount < 120)
        {
            // NOTE: Warmup.
            return;
        }
        
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(MaxFamesCount, _frameTimings);

        if (_enableFog)
        {
            AddSample(_gpuFrameTimesWithFog, ref _samplesCountWithFog, ref _frameTimings[0]);
        }
        else
        {
            AddSample(_gpuFrameTimesWithoutFog, ref _samplesCountWithoutFog, ref _frameTimings[0]);
        }
    }

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(new Vector3(3, 3, 3));

        if (Time.frameCount < 120)
        {
            // NOTE: Warmup.
            GUILayout.Label("Warmup...");
            return;
        }
        
        var avgTimeWithFog = ComputeAverageTime(_gpuFrameTimesWithFog, _samplesCountWithFog);
        var avgTimeWithoutFog = ComputeAverageTime(_gpuFrameTimesWithoutFog, _samplesCountWithoutFog);

        DrawDualLabel("GPU FrameTime (NO FOG)  : ", avgTimeWithoutFog);
        DrawDualLabel("GPU FrameTime (WITH FOG): ", avgTimeWithFog);

        _enableFog = GUILayout.Toggle(_enableFog, "Enable Fog");
        if (Volume.profile.TryGet<HeightFogSettings>(typeof(HeightFogSettings), out var settings))
        {
            settings.active = _enableFog;
        }
    }

    private static void AddSample(double[] times, ref long samplesCount, ref FrameTiming frameTiming)
    {
        times[samplesCount % times.Length] = frameTiming.gpuFrameTime;
        samplesCount++;
    }

    private static double ComputeAverageTime(double[] times, long samplesCount)
    {
        double avgTimeWithFog = 0.0;
        for (int i = 0; i < times.Length && i < samplesCount; i++)
        {
            avgTimeWithFog += times[i];
        }
        avgTimeWithFog /= Mathf.Min(times.Length, samplesCount);
        return avgTimeWithFog;
    }

    private static void DrawDualLabel(string label, double gpuFrameTime)
    {
        GUILayout.Label(label);
        var rect = GUILayoutUtility.GetLastRect();
        rect.x += rect.width;
        GUI.Label(rect, gpuFrameTime.ToString("F3", CultureInfo.InvariantCulture));

        rect.x += 40;
        GUI.Label(rect, "ms");
    }
}
