using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using HeightFog.Runtime;

namespace Demo
{
    [RequireComponent(typeof(UIDocument))]
    public class PerformanceCounterUI : MonoBehaviour
    {
        private const int MaxFramesCount = 1;
        private const int MaxSamplesCount = 1024;

        private readonly FrameTiming[] _frameTimings = new FrameTiming[MaxFramesCount];

        private int _warmUpFramesCount = 120;

        private long _samplesCountFogOff = 0;
        private readonly double[] _gpuFrameTimesFogOff = new double[MaxSamplesCount];
        private readonly double[] _cpuFrameTimesFogOff = new double[MaxSamplesCount];

        private long _samplesCountFogOn = 0;
        private readonly double[] _gpuFrameTimesFogOn = new double[MaxSamplesCount];
        private readonly double[] _cpuFrameTimesFogOn = new double[MaxSamplesCount];

        private long _gpuFogPassSamplesCount = 0;
        private readonly double[] _gpuFogPassTimes = new double[60];

        public Volume Volume;

        private string _systemInfo;
        private VisualElement _root;

        // UI Elements
        private Label _systemInfoLabel;
        private Label _warmupLabel;
        private Label _gpuFogOnLabel, _gpuFogOffLabel, _gpuDiffLabel;
        private Label _cpuFogOnLabel, _cpuFogOffLabel, _cpuDiffLabel;
        private Button _resetButton;

        private Toggle _fogToggle;
        private bool _enableFog = true;

        private Toggle _useAlphaBlendToggle;
        private bool _useAlphaBlend = false;

        private ProfilingSampler _profilingSampler;
        private Label _gpuFogPassTime;

        private void Start()
        {
            Application.targetFrameRate = 120;
            _systemInfo =
                $"Device:       {SystemInfo.deviceModel}\n" +
                $"Gfx Device:   {SystemInfo.graphicsDeviceName}\n" +
                $"Graphics API: {SystemInfo.graphicsDeviceVersion}\n" +
                $"Resolution:   {Screen.currentResolution}";

            BuildUI();
            _profilingSampler = ProfilingSampler.Get(CustomRenderFeature.HeightFogPass);
            _profilingSampler.enableRecording = true;
        }

        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();

            if (_warmUpFramesCount > 0)
            {
                _warmupLabel.text = $"Warming up... ({_warmUpFramesCount})";
                _warmUpFramesCount--;
                return;
            }

            _warmupLabel.text = "";

            var samplesCount = FrameTimingManager.GetLatestTimings(MaxFramesCount, _frameTimings);
            if (samplesCount == 0)
                return;

            if (_enableFog)
            {
                AddSample(_gpuFrameTimesFogOn, _cpuFrameTimesFogOn, ref _samplesCountFogOn, ref _frameTimings[0]);

                if (_profilingSampler != null)
                {
                    _gpuFogPassTimes[_gpuFogPassSamplesCount % _gpuFogPassTimes.Length] = _profilingSampler.gpuElapsedTime;
                    _gpuFogPassSamplesCount++;
                }
            }
            else
            {
                AddSample(_gpuFrameTimesFogOff, _cpuFrameTimesFogOff, ref _samplesCountFogOff, ref _frameTimings[0]);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            var (gpuMeanFogOn, errFogOn) = ComputeMeanAndErr(_gpuFrameTimesFogOn, _samplesCountFogOn);
            var (gpuMeanFogOff, errFogOff) = ComputeMeanAndErr(_gpuFrameTimesFogOff, _samplesCountFogOff);
            var (gpuMeanDiff, gpuErrDiff) = ComputeMeanDifference(gpuMeanFogOn, errFogOn, gpuMeanFogOff, errFogOff);

            var (cpuMeanFogOn, cpuErrFogOn) = ComputeMeanAndErr(_cpuFrameTimesFogOn, _samplesCountFogOn);
            var (cpuMeanFogOff, cpuErrFogOff) = ComputeMeanAndErr(_cpuFrameTimesFogOff, _samplesCountFogOff);
            var (cpuMeanDiff, cpuErrDiff) =
                ComputeMeanDifference(cpuMeanFogOn, cpuErrFogOn, cpuMeanFogOff, cpuErrFogOff);

            _gpuFogOnLabel.text = $"  Fog ON:     {gpuMeanFogOn:F3} ms | err: {errFogOn:F3}";
            _gpuFogOffLabel.text = $"  Fog OFF:    {gpuMeanFogOff:F3} ms | err: {errFogOff:F3}";
            _gpuDiffLabel.text = $"  Difference: {gpuMeanDiff:F3} ms | err: {gpuErrDiff:F3}";

            _cpuFogOnLabel.text = $"  Fog ON:     {cpuMeanFogOn:F3} ms | err: {cpuErrFogOn:F3}";
            _cpuFogOffLabel.text = $"  Fog OFF:    {cpuMeanFogOff:F3} ms | err: {cpuErrFogOff:F3}";
            _cpuDiffLabel.text = $"  Difference: {cpuMeanDiff:F3} ms | err: {cpuErrDiff:F3}";


            var fogPass = ComputeMeanAndErr(_gpuFogPassTimes, _gpuFogPassSamplesCount);
            _gpuFogPassTime.text = $"HeightFogPass: {fogPass.average:F3} ms | err: {fogPass.stdErr:F3}";
        }

        private void BuildUI()
        {
            var doc = GetComponent<UIDocument>();
            var scale = 0.5f;
            doc.panelSettings.scale = scale;

            var styleSheet = Resources.Load<StyleSheet>("Styles/PerformanceCounterStyle");

            _root = doc.rootVisualElement;
            _root.styleSheets.Add(styleSheet);

            _root.style.flexDirection = FlexDirection.Row;
            _root.style.alignItems = Align.FlexStart;
            _root.style.color = Color.white;
            _root.style.flexShrink = 0;
            _root.style.flexGrow = 0;

            var safeArea = Screen.safeArea;
            _root.style.paddingLeft = safeArea.xMin * scale;
            _root.style.paddingTop = safeArea.yMin * scale;
            _root.style.paddingRight = (Screen.width - safeArea.xMax) * scale;
            _root.style.paddingBottom = (Screen.height - safeArea.yMax) * scale;

            // Left panel
            var leftBox = new VisualElement();
            leftBox.AddToClassList("panel");
            _root.Add(leftBox);

            _systemInfoLabel = new Label(_systemInfo);
            _systemInfoLabel.style.whiteSpace = WhiteSpace.Normal;
            leftBox.Add(_systemInfoLabel);

            _warmupLabel = new Label("Warmup...");
            leftBox.Add(_warmupLabel);

            leftBox.Add(new Label("GPU FrameTime:"));
            _gpuFogOnLabel = new Label();
            _gpuFogOffLabel = new Label();
            _gpuDiffLabel = new Label();
            leftBox.Add(_gpuFogOnLabel);
            leftBox.Add(_gpuFogOffLabel);
            leftBox.Add(_gpuDiffLabel);

            leftBox.Add(new Label("CPU RenderThread:"));
            _cpuFogOnLabel = new Label();
            _cpuFogOffLabel = new Label();
            _cpuDiffLabel = new Label();
            leftBox.Add(_cpuFogOnLabel);
            leftBox.Add(_cpuFogOffLabel);
            leftBox.Add(_cpuDiffLabel);

            _resetButton = new Button(() =>
            {
                _samplesCountFogOn = 0;
                _samplesCountFogOff = 0;
            })
            {
                text = "Reset"
            };
            leftBox.Add(_resetButton);
            
            var middleBox = new VisualElement();
            _root.Add(middleBox);
            middleBox.AddToClassList("panel");
            middleBox.Add(new Label("GPU Frame times:"));
            _gpuFogPassTime = new Label();
            middleBox.Add(_gpuFogPassTime);

            // Right panel
            var rightBox = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0, 0, 0, 0.5f),
                    flexGrow = 0
                }
            };
            rightBox.AddToClassList("panel");
            _root.Add(rightBox);

            _fogToggle = new Toggle("Enable Fog") { value = _enableFog };
            _fogToggle.RegisterValueChangedCallback(evt =>
            {
                _enableFog = evt.newValue;
                _warmUpFramesCount = 10;
                if (Volume.profile.TryGet(out HeightFogSettings settings))
                {
                    settings.active = _enableFog;
                }
            });
            rightBox.Add(_fogToggle);

            // NOTE: UseAlphaBlend is nested by GPU type.
            // _useAlphaBlendToggle = new Toggle("Use AlphaBlend") { value = _useAlphaBlend };
            // _useAlphaBlendToggle.RegisterValueChangedCallback(evt =>
            // {
            //     _useAlphaBlend = evt.newValue;
            //     _warmUpFramesCount = 10;
            //     if (Volume.profile.TryGet(out HeightFogSettings settings))
            //     {
            //         settings.UseAlphaBlend.value = _useAlphaBlend;
            //     }
            // });
            // rightBox.Add(_useAlphaBlendToggle);
        }

        private void AddSample(double[] gpuTimes, double[] cpuTimes, ref long samplesCount, ref FrameTiming frameTiming)
        {
            if (frameTiming.gpuFrameTime > 10000.0) return;

            gpuTimes[samplesCount % gpuTimes.Length] = frameTiming.gpuFrameTime;
            cpuTimes[samplesCount % gpuTimes.Length] = frameTiming.cpuRenderThreadFrameTime;
            samplesCount++;
        }

        private static (double average, double stdErr) ComputeMeanAndErr(double[] times, long samplesCount)
        {
            int n = Mathf.Min(times.Length, (int)samplesCount);
            if (n <= 1) return (0.0, 0.0);

            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += times[i];
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
            double mean2, double stdErr2)
        {
            double meanDiff = mean1 - mean2;
            double stdErrDiff = Math.Sqrt(stdErr1 * stdErr1 + stdErr2 * stdErr2);
            return (meanDiff, stdErrDiff);
        }
    }
}
