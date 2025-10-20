#if UNITASK_INSTALLED
using System.Collections.Generic;
using static ClosureAI.AI;

namespace ClosureAI
{
    /// <summary>
    /// A dictionary-like collection that maintains insertion order and prevents duplicate keys.
    /// </summary>
    public class OrderedSnapshotDictionary
    {
        private readonly List<(Node node, NodeSnapshotData snapshot)> _values = new();
        private readonly HashSet<Node> _keys = new();

        /// <summary>
        /// Tries to add a key-value pair. Returns true if added, false if key already exists.
        /// </summary>
        public bool TryAdd(Node node, NodeSnapshotData snapshot)
        {
            if (_keys.Add(node))
            {
                _values.Add((node, snapshot));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all entries from the collection.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            _keys.Clear();
        }

        /// <summary>
        /// Gets an enumerator to iterate through the collection in insertion order.
        /// </summary>
        public IEnumerator<(Node node, NodeSnapshotData snapshot)> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        /// <summary>
        /// Gets the number of entries in the collection.
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Indexer to access entries by index in insertion order.
        /// </summary>
        public (Node node, NodeSnapshotData snapshot) this[int index] => _values[index];
    }
}



#endif