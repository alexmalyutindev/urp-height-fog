using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using HeightFog.Runtime;

public class PerformanceCounter : MonoBehaviour
{
    private const int MaxFamesCount = 1;
    private readonly FrameTiming[] _frameTimings = new FrameTiming[MaxFamesCount];

    private readonly double[] _gpuFrameTimesWithoutFog = new double[2048];
    private readonly double[] _gpuFrameTimesWithFog = new double[2048];

    public Volume Volume;

    private long _samplesCountWithFog = 0;
    private long _samplesCountWithoutFog = 0;
    private bool _enableFog = true;
    private string _systemInfo;

    private void Start()
    {
        Application.targetFrameRate = 120;
        _systemInfo = 
            $"Device: {SystemInfo.deviceModel}\n" +
            $"Gfx Device: {SystemInfo.graphicsDeviceName}\n" +
            $"Graphics API: {SystemInfo.graphicsDeviceType}\n" +
            $"Resolution: {Screen.currentResolution}";
    }

    private void Update()
    {
        if (Time.frameCount < 120)
        {
            // NOTE: Warmup.
            return;
        }

        FrameTimingManager.CaptureFrameTimings();
        var samplesCount = FrameTimingManager.GetLatestTimings(MaxFamesCount, _frameTimings);
        if (samplesCount == 0)
        {
            return;
        }

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

        var (meanTimeWithFog, errWithFog) = ComputeMeanAndErr(_gpuFrameTimesWithFog, _samplesCountWithFog);
        var (meanTimeWithoutFog, errWithoutFog) = ComputeMeanAndErr(_gpuFrameTimesWithoutFog, _samplesCountWithoutFog);
        var (meanDiff, stdErrDiff) = ComputeMeanDifference(meanTimeWithFog, errWithFog, meanTimeWithoutFog, errWithoutFog);

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.Label(_systemInfo);

            DrawStats("GPU Time (Fog ON)  :", meanTimeWithFog, errWithFog);
            DrawStats("GPU Time (Fog OFF) :", meanTimeWithoutFog, errWithoutFog);
            DrawStats("GPU Time Difference:", meanDiff, stdErrDiff);

            if (GUILayout.Button("Reset"))
            {
                _samplesCountWithFog = 0;
                _samplesCountWithoutFog = 0;
            }
        }

        _enableFog = GUILayout.Toggle(_enableFog, "Enable Fog");
        if (Volume.profile.TryGet<HeightFogSettings>(typeof(HeightFogSettings), out var settings))
        {
            settings.active = _enableFog;
        }
    }

    private static void DrawStats(string label, double mean, double err)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(label);
            GUILayout.Label(mean.ToString("F3", CultureInfo.InvariantCulture));
            GUILayout.Label("ms");
            GUILayout.Label("err:");
            GUILayout.Label(err.ToString("F3", CultureInfo.InvariantCulture));
            GUILayout.Label("ms");
        }
    }

    private static void AddSample(double[] times, ref long samplesCount, ref FrameTiming frameTiming)
    {
        times[samplesCount % times.Length] = frameTiming.gpuFrameTime;
        samplesCount++;
    }

    private static (double average, double stdErr) ComputeMeanAndErr(double[] times, long samplesCount)
    {
        int n = Mathf.Min(times.Length, (int)samplesCount);
        if (n <= 1)
        {
            return (0.0, 0.0);
        }

        double sum = 0.0;
        for (int i = 0; i < n; i++)
        {
            sum += times[i];
        }
        double mean = sum / n;

        double varianceSum = 0.0;
        for (int i = 0; i < n; i++)
        {
            double diff = times[i] - mean;
            varianceSum += diff * diff;
        }

        double variance = varianceSum / (n - 1);
        double stdDev = Math.Sqrt(variance);
        double stdErr = stdDev / Mathf.Sqrt(n);

        return (mean, stdErr);
    }

    public static (double meanDiff, double stdErrDiff) ComputeMeanDifference(
        double mean1, double stdErr1,
        double mean2, double stdErr2
    )
    {
        double meanDiff = mean1 - mean2;
        double stdErrDiff = Math.Sqrt(stdErr1 * stdErr1 + stdErr2 * stdErr2);
        return (meanDiff, stdErrDiff);
    }
}
