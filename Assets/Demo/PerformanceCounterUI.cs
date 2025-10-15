using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using HeightFog.Runtime;

namespace Demo
{
    [RequireComponent(typeof(UIDocument))]
    public class PerformanceCounterUI : MonoBehaviour
    {
        private const int MaxFramesCount = 1;
        private readonly FrameTiming[] _frameTimings = new FrameTiming[MaxFramesCount];

        private readonly int _warmUpFramesCount = 120;
        private readonly double[] _gpuFrameTimesFogOff = new double[2048];
        private readonly double[] _gpuFrameTimesFogOn = new double[2048];
        private readonly double[] _cpuFrameTimesFogOff = new double[2048];
        private readonly double[] _cpuFrameTimesFogOn = new double[2048];

        public Font Font;
        public Volume Volume;

        private long _samplesCountFogOn = 0;
        private long _samplesCountFogOff = 0;
        private bool _enableFog = true;

        // UI Elements
        private Label _systemInfoLabel;
        private Label _warmupLabel;
        private Label _gpuFogOnLabel, _gpuFogOffLabel, _gpuDiffLabel;
        private Label _cpuFogOnLabel, _cpuFogOffLabel, _cpuDiffLabel;
        private Button _resetButton;
        private Toggle _fogToggle;

        private VisualElement _root;
        private string _systemInfo;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            _systemInfo =
                $"Device:       {SystemInfo.deviceModel}\n" +
                $"Gfx Device:   {SystemInfo.graphicsDeviceName}\n" +
                $"Graphics API: {SystemInfo.graphicsDeviceVersion}\n" +
                $"Resolution:   {Screen.currentResolution}";

            BuildUI();
        }

        private void Update()
        {
            if (Time.frameCount < _warmUpFramesCount)
            {
                _warmupLabel.text = $"Warming up... ({Time.frameCount}/{_warmUpFramesCount})";
                return;
            }

            _warmupLabel.text = "";

            FrameTimingManager.CaptureFrameTimings();
            var samplesCount = FrameTimingManager.GetLatestTimings(MaxFramesCount, _frameTimings);
            if (samplesCount == 0)
                return;

            if (_enableFog)
            {
                AddSample(_gpuFrameTimesFogOn, _cpuFrameTimesFogOn, ref _samplesCountFogOn, ref _frameTimings[0]);
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
            var (cpuMeanDiff, cpuErrDiff) = ComputeMeanDifference(cpuMeanFogOn, cpuErrFogOn, cpuMeanFogOff, cpuErrFogOff);

            _gpuFogOnLabel.text =  $"  Fog ON:     {gpuMeanFogOn:F3} ms | err: {errFogOn:F3}";
            _gpuFogOffLabel.text = $"  Fog OFF:    {gpuMeanFogOff:F3} ms | err: {errFogOff:F3}";
            _gpuDiffLabel.text =   $"  Difference: {gpuMeanDiff:F3} ms | err: {gpuErrDiff:F3}";

            _cpuFogOnLabel.text =  $"  Fog ON:     {cpuMeanFogOn:F3} ms | err: {cpuErrFogOn:F3}";
            _cpuFogOffLabel.text = $"  Fog OFF:    {cpuMeanFogOff:F3} ms | err: {cpuErrFogOff:F3}";
            _cpuDiffLabel.text =   $"  Difference: {cpuMeanDiff:F3} ms | err: {cpuErrDiff:F3}";
        }

        private void BuildUI()
        {
            var doc = GetComponent<UIDocument>();
            doc.panelSettings.scale = 0.5f;

            var styleSheet = Resources.Load<StyleSheet>("Styles/PerformanceCounterStyle");

            _root = doc.rootVisualElement;
            _root.styleSheets.Add(styleSheet);

            _root.style.flexDirection = FlexDirection.Row;
            _root.style.alignItems = Align.FlexStart;
            _root.style.color = Color.white;
            _root.style.flexShrink = 0;
            _root.style.flexGrow = 0;

            // Left panel
            var leftBox = new VisualElement
            {
                // style =
                // {
                //     alignItems = Align.FlexStart,
                //     flexDirection = FlexDirection.Column,
                //     justifyContent = Justify.FlexStart,
                //     flexGrow = 0,
                //     flexShrink = 0,
                //     height = StyleKeyword.Auto,
                //     width = StyleKeyword.Auto,
                //     paddingRight = 6,
                //     paddingLeft = 6,
                //     paddingTop = 6,
                //     paddingBottom = 6,
                //     backgroundColor = new Color(0, 0, 0, 0.5f),
                // }
            };
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
            
            // Spacer
            _root.Add(new VisualElement() { style = { width = 4, flexShrink = 0 } });

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
                if (Volume.profile.TryGet(out HeightFogSettings settings))
                {
                    settings.active = _enableFog;
                }
            });
            rightBox.Add(_fogToggle);
        }

        private static void AddSample(double[] gpuTimes, double[] cpuTimes, ref long samplesCount, ref FrameTiming frameTiming)
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
