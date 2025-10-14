using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using HeightFog.Runtime;

public class PerformanceCounter : MonoBehaviour
{
    private const int MaxFamesCount = 1;
    private readonly FrameTiming[] _frameTimings = new FrameTiming[MaxFamesCount];

    private readonly int _warmUpFramesCount = 120;
    private readonly double[] _gpuFrameTimesFogOff = new double[2048];
    private readonly double[] _gpuFrameTimesFogOn = new double[2048]; 
    
    private readonly double[] _cpuFrameTimesFogOff = new double[2048];
    private readonly double[] _cpuFrameTimesFogOn = new double[2048];

    public Volume Volume;
    public Font Font;

    private long _samplesCountFogOn = 0;
    private long _samplesCountFogOff = 0;
    private bool _enableFog = true;
    private string _systemInfo;

    private void Start()
    {
        Application.targetFrameRate = 120;
        _systemInfo =
            $"Device:       {SystemInfo.deviceModel}\n" +
            $"Gfx Device:   {SystemInfo.graphicsDeviceName}\n" +
            $"Graphics API: {SystemInfo.graphicsDeviceVersion}\n" +
            $"Resolution:   {Screen.currentResolution}";
    }

    private void Update()
    {
        if (Time.frameCount < _warmUpFramesCount)
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
            AddSample(_gpuFrameTimesFogOn, _cpuFrameTimesFogOn, ref _samplesCountFogOn, ref _frameTimings[0]);
        }
        else
        {
            AddSample(_gpuFrameTimesFogOff, _cpuFrameTimesFogOff, ref _samplesCountFogOff, ref _frameTimings[0]);
        }
    }

    private void OnGUI()
    {
        var guiScale = 2.0f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, guiScale));
        GUI.skin.font = Font;

        var (gpuMeanFogOn, errFogOn) = ComputeMeanAndErr(_gpuFrameTimesFogOn, _samplesCountFogOn);
        var (gpuMeanFogOff, errFogOff) = ComputeMeanAndErr(_gpuFrameTimesFogOff, _samplesCountFogOff);
        var (gpuMeanDiff, gpuErrDiff) = ComputeMeanDifference(
            gpuMeanFogOn, errFogOn, 
            gpuMeanFogOff, errFogOff
        );

        var (cpuMeanFogOn, cpuErrFogOn) = ComputeMeanAndErr(_cpuFrameTimesFogOn, _samplesCountFogOn);
        var (cpuMeanFogOff, cpuErrFogOff) = ComputeMeanAndErr(_cpuFrameTimesFogOff, _samplesCountFogOff);
        var (cpuMeanDiff, cpuErrDiff) = ComputeMeanDifference(
            cpuMeanFogOn, cpuErrFogOn, 
            cpuMeanFogOff, cpuErrFogOff
        );

        var rect = new Rect(Screen.safeArea);
        rect.x /= guiScale;
        rect.y /= guiScale;
        rect.width /= guiScale;
        rect.height /= guiScale;

        using (new GUILayout.AreaScope(rect))
        {
            using var _ = new GUILayout.HorizontalScope();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(_systemInfo);
                
                if (Time.frameCount < _warmUpFramesCount)
                {
                    GUILayout.Label("Warmup...");
                }
                else
                {
                    GUILayout.Label("GPU FrameTime:");
                    DrawStats("  Fog ON:    ", gpuMeanFogOn, errFogOn);
                    DrawStats("  Fog OFF:   ", gpuMeanFogOff, errFogOff);
                    DrawStats("  Difference:", gpuMeanDiff, gpuErrDiff);

                    GUILayout.Label("CPU RenderThread:");
                    DrawStats("  Fog ON:    ", cpuMeanFogOn, cpuErrFogOn);
                    DrawStats("  Fog OFF:   ", cpuMeanFogOff, cpuErrFogOff);
                    DrawStats("  Difference:", cpuMeanDiff, cpuErrDiff);

                    GUILayout.Space(2);
                    if (GUILayout.Button("Reset"))
                    {
                        _samplesCountFogOn = 0;
                        _samplesCountFogOff = 0;
                    }
                }
            }

            GUILayout.FlexibleSpace();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                _enableFog = GUILayout.Toggle(_enableFog, "Enable Fog");
                if (Volume.profile.TryGet<HeightFogSettings>(typeof(HeightFogSettings), out var settings))
                {
                    settings.active = _enableFog;
                }
            }
        }
    }

    private static void DrawStats(string label, double mean, double err)
    {
        using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
        {
            GUILayout.Label(label);
            GUILayout.Label(mean.ToString("F3", CultureInfo.InvariantCulture));
            GUILayout.Label("ms");
            GUILayout.Label(" | err:");
            GUILayout.Label(err.ToString("F3", CultureInfo.InvariantCulture));
            GUILayout.Label("ms");
        }
        GUILayout.Space(-2);
    }

    private static void AddSample(double[] gpuTimes, double[] cpuTimes, ref long samplesCount, ref FrameTiming frameTiming)
    {
        // BUG: On Vulkan somethings it samples incorrect values! Skip it. 
        if (frameTiming.gpuFrameTime > 10000.0) return;

        gpuTimes[samplesCount % gpuTimes.Length] = frameTiming.gpuFrameTime;
        cpuTimes[samplesCount % gpuTimes.Length] = frameTiming.cpuRenderThreadFrameTime;
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
