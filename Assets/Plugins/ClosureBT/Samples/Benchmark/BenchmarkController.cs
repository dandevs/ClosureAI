using System.Collections.Generic;
using System.Diagnostics;
using ClosureBT;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Samples.Benchmark
{
    /// <summary>
    /// Manages benchmark node instances and tracks performance metrics
    /// </summary>
    public class BenchmarkController
    {
        private readonly List<Node> _nodes = new();
        private readonly Dictionary<Node, int> _nodeCounts = new();
        private readonly Stopwatch _tickStopwatch = new();
        private readonly Queue<float> _fpsSamples = new();
        private readonly Queue<float> _tickTimeSamples = new();
        private float _sampleTimer;
        private float _statsDelayTimer = 1f;
        private const int MaxSamples = 10;
        private const float SampleInterval = 0.1f;
        private const float StatsCollectionDelay = 1f;

        public int NodeCount => _nodes.Count;
        public int TotalNodeCount { get; private set; }
        public float FPS { get; private set; }
        public float FPSMin { get; private set; } = float.MaxValue;
        public float FPSMax { get; private set; } = float.MinValue;
        public float TickTimeMs { get; private set; }
        public float TickTimeMsMin { get; private set; } = float.MaxValue;
        public float TickTimeMsMax { get; private set; } = float.MinValue;

        /// <summary>
        /// Creates and adds the specified number of benchmark nodes
        /// </summary>
        public void AddNodes(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var newNode = CreateBenchmarkNode();
                _nodes.Add(newNode);

                // Incrementally count nodes for this specific root
                int subNodeCount = 0;
                Node.Traverse(newNode, _ => subNodeCount++);
                _nodeCounts[newNode] = subNodeCount;
                TotalNodeCount += subNodeCount;
            }

            // Reset min-max counters and delay collection after node count changes
            _tickTimeSamples.Clear();
            TickTimeMs = 0;
            FPSMin = float.MaxValue;
            FPSMax = float.MinValue;
            TickTimeMsMin = float.MaxValue;
            TickTimeMsMax = float.MinValue;
            _statsDelayTimer = StatsCollectionDelay;
        }

        /// <summary>
        /// Clears all nodes immediately
        /// </summary>
        public void Reset()
        {
            foreach (var node in _nodes)
            {
                node.ResetImmediately();
            }
            _nodes.Clear();
            _nodeCounts.Clear();
            _fpsSamples.Clear();
            _tickTimeSamples.Clear();
            _sampleTimer = 0;
            _statsDelayTimer = StatsCollectionDelay;
            FPS = 0;
            FPSMin = float.MaxValue;
            FPSMax = float.MinValue;
            TickTimeMs = 0;
            TickTimeMsMin = float.MaxValue;
            TickTimeMsMax = float.MinValue;
            TotalNodeCount = 0;
        }

        /// <summary>
        /// Ticks all nodes and measures performance metrics
        /// </summary>
        public void Tick()
        {
            // Measure tick time
            _tickStopwatch.Restart();

            foreach (var node in _nodes)
            {
                node.Tick();
            }

            _tickStopwatch.Stop();
            float currentTickTimeMs = (float)_tickStopwatch.Elapsed.TotalMilliseconds;

            // Update rolling averages
            _sampleTimer += Time.deltaTime;
            if (_sampleTimer >= SampleInterval)
            {
                _sampleTimer -= SampleInterval;

                // FPS
                float currentFps = 1f / Mathf.Max(Time.deltaTime, 0.0001f);
                UpdateRollingAverage(_fpsSamples, currentFps, v => FPS = v);

                // Tick Time - only update if we have nodes
                if (_nodes.Count > 0)
                {
                    UpdateRollingAverage(_tickTimeSamples, currentTickTimeMs, v => TickTimeMs = v);
                }

                // Delay min/max collection by 1 second
                if (_statsDelayTimer > 0)
                {
                    _statsDelayTimer -= SampleInterval;
                }
                else
                {
                    if (FPS > 0)
                    {
                        FPSMin = Mathf.Min(FPSMin, FPS);
                        FPSMax = Mathf.Max(FPSMax, FPS);
                    }

                    if (_nodes.Count > 0)
                    {
                        TickTimeMsMin = Mathf.Min(TickTimeMsMin, TickTimeMs);
                        TickTimeMsMax = Mathf.Max(TickTimeMsMax, TickTimeMs);
                    }
                }
            }
        }

        private void UpdateRollingAverage(Queue<float> queue, float newValue, System.Action<float> setter)
        {
            queue.Enqueue(newValue);
            if (queue.Count > MaxSamples)
            {
                queue.Dequeue();
            }

            float sum = 0;
            foreach (var sample in queue)
            {
                sum += sample;
            }
            setter(sum / queue.Count);
        }

        /// <summary>
        /// Cleans up all nodes
        /// </summary>
        public void Cleanup()
        {
            Reset();
        }

        /// <summary>
        /// Creates a benchmark node with various operations
        /// </summary>
        private Node CreateBenchmarkNode()
        {
            return D.Repeat() + Reactive * SequenceAlways(() =>
            {
                Wait(0.1f);

                Sequence(() =>
                {
                    OnEnter(async ct =>
                    {
                        await UniTask.WaitForSeconds(0.1f);
                    });

                    OnExit(async ct =>
                    {
                        await UniTask.WaitForSeconds(0.1f);
                    });

                    Condition(() => Random.value > 0.5f);
                    Wait(0.1f);
                });

                _ = Reactive * Selector(() =>
                {
                    Condition(() => Random.value > 0.8f);
                    Sequence(() =>
                    {
                        Condition(() => Random.value > 0.3f);
                        Wait(0.05f);
                    });
                    Do(() => { var y = Mathf.Cos(Time.time * 2f); });
                });

                Do(() => { var x = Mathf.Sin(Time.time); });
            });
        }
    }
}
