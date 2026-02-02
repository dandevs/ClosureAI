using ClosureBT.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static ClosureBT.UI.VisualElementBuilderHelper;

namespace ClosureBT.Samples.Benchmark
{
    /// <summary>
    /// UI view for the benchmark system using the E() builder pattern
    /// </summary>
    public class BenchmarkUIView
    {
        private readonly BenchmarkController _controller;
        private Label _nodeCountLabel;
        private Label _totalNodeCountLabel;
        private Label _fpsLabel;
        private Label _fpsMinMaxLabel;
        private Label _tickTimeLabel;
        private Label _tickTimeMinMaxLabel;

        public BenchmarkUIView(VisualElement root, BenchmarkController controller)
        {
            _controller = controller;
            BuildUI(root);
        }

        private void BuildUI(VisualElement root)
        {
            E(root, _ =>
            {
                Style(new()
                {
                    padding = 20,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f)
                });

                E<FlexColumn>(_ =>
                {
                    FlexGap(16);

                    // Header
                    E<Label>(label =>
                    {
                        label.text = "ClosureBT Benchmark";
                        Style(new()
                        {
                            fontSize = 24,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            color = Color.white,
                            marginBottom = 8
                        });
                    });

                    // Metrics Row
                    E<FlexRow>(_ =>
                    {
                        FlexGap(16);
                        Style(new()
                        {
                            marginBottom = 8
                        });

                        _nodeCountLabel = CreateMetricLabel("Root Nodes:");
                        _totalNodeCountLabel = CreateMetricLabel("Total Node Count:");
                        (_fpsLabel, _fpsMinMaxLabel) = CreateMetricLabelWithMinMax("FPS:");
                        (_tickTimeLabel, _tickTimeMinMaxLabel) = CreateMetricLabelWithMinMax("Tick Time:");

                        // Setup reactive updates within the E() context
                        Scheduler.Execute(() =>
                        {
                            UpdateMetrics();
                        }).Every(0); // Update every frame
                    });

                    // Buttons Row
                    E<FlexRow>(_ =>
                    {
                        FlexGap(8);

                        CreateButton("+1", () => _controller.AddNodes(1));
                        CreateButton("+5", () => _controller.AddNodes(5));
                        CreateButton("+10", () => _controller.AddNodes(10));
                        CreateButton("+25", () => _controller.AddNodes(25));
                        CreateButton("+100", () => _controller.AddNodes(100));
                        CreateButton("Reset", _controller.Reset, new Color(0.8f, 0.3f, 0.3f, 1f));
                    });
                });
            });
        }

        private Label CreateMetricLabel(string labelText)
        {
            Label valueLabel = null;

            E<FlexRow>(row =>
            {
                FlexGap(4);

                E<Label>(label =>
                {
                    label.text = labelText;
                    Style(new()
                    {
                        fontSize = 14,
                        color = new Color(0.7f, 0.7f, 0.7f, 1f)
                    });
                });

                E<Label>(label =>
                {
                    valueLabel = label;
                    label.text = "0";
                    Style(new()
                    {
                        fontSize = 14,
                        color = Color.white,
                        unityFontStyleAndWeight = FontStyle.Bold
                    });
                });
            });

            return valueLabel;
        }

        private (Label valueLabel, Label minMaxLabel) CreateMetricLabelWithMinMax(string labelText)
        {
            Label valueLabel = null;
            Label minMaxLabel = null;

            E<FlexRow>(row =>
            {
                FlexGap(4);

                E<Label>(label =>
                {
                    label.text = labelText;
                    Style(new()
                    {
                        fontSize = 14,
                        color = new Color(0.7f, 0.7f, 0.7f, 1f)
                    });
                });

                E<Label>(label =>
                {
                    valueLabel = label;
                    label.text = "0";
                    Style(new()
                    {
                        fontSize = 14,
                        color = Color.white,
                        unityFontStyleAndWeight = FontStyle.Bold
                    });
                });

                E<Label>(label =>
                {
                    minMaxLabel = label;
                    label.text = "[min: -][max: -]";
                    Style(new()
                    {
                        fontSize = 12,
                        color = new Color(0.6f, 0.85f, 1f, 1f) // Light cyan color
                    });
                });
            });

            return (valueLabel, minMaxLabel);
        }

        private void CreateButton(string text, System.Action onClick, Color? backgroundColor = null)
        {
            E<Button>(button =>
            {
                button.text = text;
                button.clicked += onClick;

                Style(new()
                {
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 8,
                    paddingBottom = 8,
                    backgroundColor = backgroundColor ?? new Color(0.3f, 0.5f, 0.8f, 1f),
                    color = Color.white,
                    fontSize = 14,
                    borderRadius = 4
                });
            });
        }

        private void UpdateMetrics()
        {
            if (_nodeCountLabel != null)
                _nodeCountLabel.text = _controller.NodeCount.ToString();

            if (_totalNodeCountLabel != null)
                _totalNodeCountLabel.text = _controller.TotalNodeCount.ToString();

            if (_fpsLabel != null)
                _fpsLabel.text = _controller.FPS.ToString("F1");

            if (_fpsMinMaxLabel != null)
            {
                string minStr = _controller.FPSMin < float.MaxValue ? _controller.FPSMin.ToString("F1") : "-";
                string maxStr = _controller.FPSMax > float.MinValue ? _controller.FPSMax.ToString("F1") : "-";
                _fpsMinMaxLabel.text = $"[min: {minStr}][max: {maxStr}]";
            }

            if (_tickTimeLabel != null)
                _tickTimeLabel.text = $"{_controller.TickTimeMs:F2}ms";

            if (_tickTimeMinMaxLabel != null)
            {
                string minStr = _controller.TickTimeMsMin < float.MaxValue ? $"{_controller.TickTimeMsMin:F2}" : "-";
                string maxStr = _controller.TickTimeMsMax > float.MinValue ? $"{_controller.TickTimeMsMax:F2}" : "-";
                _tickTimeMinMaxLabel.text = $"[min: {minStr}ms][max: {maxStr}ms]";
            }
        }
    }
}
