#if UNITASK_INSTALLED
using System.Collections.Generic;
using static ClosureBT.BT;

namespace ClosureBT
{
    public struct NodeSnapshotData
    {
        public List<(Status, SubStatus)> NodeStatuses;
        public List<object> VariableValues;

        #if UNITY_EDITOR
        public List<List<Node>> Children;
        public List<YieldNode> YieldedNodes;
        #endif
    }
}

#endif
