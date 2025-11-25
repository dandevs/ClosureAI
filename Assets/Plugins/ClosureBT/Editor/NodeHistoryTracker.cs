#if UNITASK_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Editor
{
    [InitializeOnLoad]
    public static partial class NodeHistoryTracker
    {
        //******************************************************************************************
        #region Fields

        public static readonly List<List<(Node node, NodeSnapshotData snapshot)>> Snapshots = new();
        private static readonly OrderedSnapshotDictionary _originalData = new();
        private static readonly Dictionary<Node, YieldNode[]> _originalYieldedNodes = new();

        private static int _currentGlobalSnapshotIndex;
        private static bool _isShowingHistory;

        public static EndOfFrameExecutor EOF { get; private set; }
        public static event Action OnSnapshotIndexChanged = delegate {};

        private static readonly List<Action> _reverts = new();

        #endregion

        //******************************************************************************************
        #region Initialization

// #if UNITY_EDITOR
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         private static void Create()
//         {
//             EOF = new GameObject("ClosureBT / Editor " + nameof(EndOfFrameExecutor)).AddComponent<EndOfFrameExecutor>();
//             GameObject.DontDestroyOnLoad(EOF);
//         }
// #endif

        static NodeHistoryTracker()
        {
            Node.EditorOnly.OnRootNodeActivated += OnRootNodeActivated;
            Node.EditorOnly.OnRootNodeDeactivated += OnRootNodeDeactivated;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion

        //******************************************************************************************
        #region Properties

        public static int CurrentGlobalSnapshotIndex
        {
            get => _currentGlobalSnapshotIndex;
            set
            {
                var maxIndex = Mathf.Max(0, NodeSnapshotHolder.SnapshotGlobalIndex - 1);
                _currentGlobalSnapshotIndex = Mathf.Clamp(value, 0, maxIndex);
                OnSnapshotIndexChanged();
            }
        }

        public static bool IsShowingHistory => _isShowingHistory;
        public static readonly AsyncReactiveProperty<bool> IsShowingHistoryAsync = new(false);

        #endregion

        //******************************************************************************************
        #region Event Handlers

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Clear all snapshot data when entering play mode
            NodeSnapshotHolder.ResetGlobalIndex();
            Snapshots.Clear();
            _originalData.Clear();
            _reverts.Clear();
            _currentGlobalSnapshotIndex = 0;
            _isShowingHistory = false;
            IsShowingHistoryAsync.Value = false;
        }

        private static void OnRootNodeActivated(Node node)
        {
            if (Preferences.RecordSnapshots)
            {
                node.Editor.OnChildrenStatusChanged += OnNodeStatusChanged;
                node.Editor.OnExceptionThrown += OnNodeThrowException;
            }
        }

        private static void OnRootNodeDeactivated(Node node)
        {
            if (Preferences.RecordSnapshots)
            {
                node.Editor.OnChildrenStatusChanged -= OnNodeStatusChanged;
                node.Editor.OnExceptionThrown -= OnNodeThrowException;
            }
        }

        // private static void OnNodeStatusChanged(Node Node) => ScheduleSnapshotForEOF();
        private static void OnNodeStatusChanged(Node node)
        {
            if (!Preferences.RecordSnapshots)
                return;

            // Check if the current status/substatus matches any of the enabled flags
            var flags = Preferences.RecordOnStatusChangeFlags;
            var shouldRecord = false;

            // Check SubStatus flags
            switch (node.SubStatus)
            {
                case SubStatus.Entering when (flags & NodeStatusChangeFlags.Entering) != 0:
                case SubStatus.Running when (flags & NodeStatusChangeFlags.Running) != 0:
                case SubStatus.Exiting when (flags & NodeStatusChangeFlags.Exiting) != 0:
                case SubStatus.Succeeding when (flags & NodeStatusChangeFlags.Succeeding) != 0:
                case SubStatus.Failing when (flags & NodeStatusChangeFlags.Failing) != 0:
                case SubStatus.Done when (flags & NodeStatusChangeFlags.Done) != 0:
                    shouldRecord = true;
                    break;
            }

            switch (node.Status)
            {
                case Status.Failure when (flags & NodeStatusChangeFlags.Failed) != 0:
                case Status.Success when (flags & NodeStatusChangeFlags.Succeeded) != 0:
                case Status.None when (flags & NodeStatusChangeFlags.ToNone) != 0:
                    shouldRecord = true;
                    break;
            }

            // Check Status.Success flag
            if (!shouldRecord && node.Status == Status.Success && (flags & NodeStatusChangeFlags.Success) != 0)
            {
                shouldRecord = true;
            }

            if (shouldRecord)
            {
                // Debug.Log($"Changed status of node: {node.RootEditor.Node.Name} {NodeSnapshotHolder.SnapshotGlobalIndex}");
                NodeSnapshotHolder.Get(node.RootEditor.Node).CreateSnapshot();
            }
        }

        private static void OnNodeThrowException(Node node)
        {
            NodeSnapshotHolder.Get(node.RootEditor.Node).CreateSnapshot();
        }

        private static void OnPauseStateChanged(PauseState state)
        {
            if (state == PauseState.Unpaused)
                Resume();
        }

        #endregion

        //******************************************************************************************
        #region Snapshot Scheduling

        // private static void ScheduleSnapshotForEOF()
        // {
        //     if (!_delayCallScheduled)
        //     {
        //         _delayCallScheduled = true;
        //         EOF.Schedule(_CreateSnapshotForEOF);
        //     }
        // }

        // private static void _CreateSnapshotForEOF()
        // {
        //     CreateSnapshot();
        //     _delayCallScheduled = false;
        // }

        #endregion

        //******************************************************************************************
        #region Snapshot Navigation

        private static void InvokeReverts()
        {
            foreach (var revert in _reverts)
                revert.Invoke();

            _reverts.Clear();
        }

        public static void ChangeGlobalSnapshotIndex(int index)
        {
            InvokeReverts();

            var maxGlobalIndex = NodeSnapshotHolder.SnapshotGlobalIndex - 1;
            var newIndex = Mathf.Clamp(index, 0, Mathf.Max(maxGlobalIndex, 0));
            CurrentGlobalSnapshotIndex = newIndex;

            if (!_isShowingHistory)
            {
                // Store original data for all nodes before showing history
                _originalData.Clear();
                _originalYieldedNodes.Clear();

                foreach (var rootNode in Node.EditorOnly.RootNodes)
                    _originalData.TryAdd(rootNode, rootNode.CreateSnapshot());

                foreach (var rootNode in Node.EditorOnly.RootNodes)
                    _originalYieldedNodes.TryAdd(rootNode, rootNode.Editor.YieldNodes.ToArray());

                EditorApplication.isPaused = true;
                _isShowingHistory = true;
                IsShowingHistoryAsync.Value = true;
            }

            // Load the snapshot data for all nodes at this global index
            if (_isShowingHistory)
            {
                foreach (var rootNode in Node.EditorOnly.RootNodes)
                {
                    if (Node.IsInvalid(rootNode))
                        continue;

                    var snapshotHolder = NodeSnapshotHolder.Get(rootNode);

                    // Try exact match first, then fall back to nearest snapshot
                    if (snapshotHolder.TryGetSnapshotGlobalIndex(newIndex, out var snapshotData))
                    {
                        rootNode.Load(snapshotData, out var revert, _originalData);

                        if (revert != null)
                            _reverts.Add(revert);
                    }
                    else if (snapshotHolder.TryGetNearestSnapshot(newIndex, out var nearestEntry, out _))
                    {
                        // Load the nearest snapshot to reflect the most accurate state at this time
                        rootNode.Load(nearestEntry.Snapshot, out var revert, _originalData);

                        if (revert != null)
                            _reverts.Add(revert);
                    }
                }
            }
        }

        public static void Resume()
        {
            if (!_isShowingHistory)
                return;

            InvokeReverts();

            // Restore original data for all nodes
            foreach (var (node, snapshot) in _originalData)
                node.Load(snapshot);

            // Restore original yielded nodes
            foreach (var (node, yieldedNodes) in _originalYieldedNodes)
            {
                node.RootEditor.YieldNodes.Clear();
                node.RootEditor.YieldNodes.AddRange(yieldedNodes);
            }

            _originalData.Clear();
            _originalYieldedNodes.Clear();
            _currentGlobalSnapshotIndex = 0;
            _isShowingHistory = false;
            IsShowingHistoryAsync.Value = false;

            EditorApplication.isPaused = false;
        }

        public static void NextSnapshot() => ChangeGlobalSnapshotIndex(CurrentGlobalSnapshotIndex + 1);
        public static void PreviousSnapshot() => ChangeGlobalSnapshotIndex(CurrentGlobalSnapshotIndex - 1);

        #endregion
    }

    //******************************************************************************************
    #region Enums

    public enum NodeCaptureMode
    {
        Independent, // Per node scrubbing only, will not reflect changes of other nodes that are recorded
        Collective, // Collective will allow scrubbing of multiple nodes at the same time
    }

    #endregion
}

#endif
