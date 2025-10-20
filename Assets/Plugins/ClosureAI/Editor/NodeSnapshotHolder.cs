#if UNITASK_INSTALLED
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ClosureAI.AI;

#if UNITY_EDITOR
namespace ClosureAI.Editor
{
    public class NodeSnapshotHolder
    {
        private static readonly ConditionalWeakTable<Node, NodeSnapshotHolder> _trackers = new();

        public readonly Node Node;
        public readonly List<NodeHistoryTrackerEntry> Entries = new();
        private readonly Dictionary<int, NodeHistoryTrackerEntry> _snapshotCache = new();
        public static int SnapshotGlobalIndex { get; private set; } = 0;

        public NodeSnapshotHolder(Node node)
        {
            Node = node;
        }

        /// <summary>
        /// Static initialization method called after domain reload.
        /// Re-initializes all existing trackers.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            Node.EditorOnly.OnRootNodeActivated -= OnRootNodeActivated;
            Node.EditorOnly.OnRootNodeActivated += OnRootNodeActivated;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnRuntimeInitialize()
        {
            SnapshotGlobalIndex = 0;
        }

        private static void OnRootNodeActivated(Node node)
        {
            if (!_trackers.TryGetValue(node, out var _))
                _trackers.Add(node, new(node));
        }

        public static NodeSnapshotHolder Get(Node node)
        {
            if (!_trackers.TryGetValue(node, out var tracker))
            {
                tracker = new(node);
                _trackers.Add(node, tracker);
            }

            return tracker;
        }

        public static void ResetGlobalIndex()
        {
            // SnapshotGlobalIndex = 0;
        }

        public void CreateSnapshot()
        {
            var entry = new NodeHistoryTrackerEntry()
            {
                GlobalIndex = SnapshotGlobalIndex,
                Snapshot = Node.CreateSnapshot(),
            };

            Entries.Add(entry);

            _snapshotCache[entry.GlobalIndex] = entry;
            // Debug.Log($"Entry Created (GI {SnapshotGlobalIndex})  (Count {Entries.Count})");
            SnapshotGlobalIndex++;

            while (Entries.Count > Preferences.MaxRecordedSnapshots)
            {
                var removedEntry = Entries[0];
                _snapshotCache.Remove(removedEntry.GlobalIndex);
                Entries.RemoveAt(0);
            }
        }

        public NodeSnapshotData GetSnapshotLocalIndex(int frame)
        {
            return Entries[frame].Snapshot;
        }

        public bool TryGetSnapshotGlobalIndex(int frame, out NodeSnapshotData snapshot)
        {
            if (TryGetEntryAtGlobalIndex(frame, out var entry))
            {
                snapshot = entry.Snapshot;
                return true;
            }

            snapshot = default;
            return false;
        }

        public bool TryGetEntryAtGlobalIndex(int globalIndex, out NodeHistoryTrackerEntry entry)
        {
            if (_snapshotCache.TryGetValue(globalIndex, out entry))
                return true;

            for (var i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];

                if (e.GlobalIndex == globalIndex)
                {
                    entry = e;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public bool TryGetNearestSnapshot(int requestedGlobalIndex, out NodeHistoryTrackerEntry entry, out int localIndex)
        {
            if (Entries.Count == 0)
            {
                entry = default;
                localIndex = -1;
                return false;
            }

            if (_snapshotCache.TryGetValue(requestedGlobalIndex, out entry))
            {
                localIndex = BinarySearchByGlobalIndex(requestedGlobalIndex);
                if (localIndex < 0)
                {
                    localIndex = 0;
                }
                return true;
            }

            var searchIndex = BinarySearchByGlobalIndex(requestedGlobalIndex);

            if (searchIndex >= 0)
            {
                localIndex = searchIndex;
                entry = Entries[localIndex];
                return true;
            }

            var insertionPoint = ~searchIndex;

            if (insertionPoint >= Entries.Count)
            {
                localIndex = Entries.Count - 1;
                entry = Entries[localIndex];
                return true;
            }

            if (insertionPoint == 0)
            {
                localIndex = 0;
                entry = Entries[localIndex];
                return true;
            }

            localIndex = insertionPoint;
            entry = Entries[localIndex];
            return true;
        }

        private int BinarySearchByGlobalIndex(int globalIndex)
        {
            var left = 0;
            var right = Entries.Count - 1;

            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var midValue = Entries[mid].GlobalIndex;

                if (midValue == globalIndex)
                    return mid;

                if (midValue < globalIndex)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return ~left;
        }
    }

    public struct NodeHistoryTrackerEntry
    {
        public int GlobalIndex;
        public NodeSnapshotData Snapshot;
    }
}
#endif

#endif