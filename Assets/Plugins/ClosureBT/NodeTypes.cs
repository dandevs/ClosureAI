#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        [Serializable]
        public class LeafNode : Node
        {
            public LeafNode(string name) : base(name) { }
        }

        [Serializable]
        public class CompositeNode : Node
        {
            [SerializeReference] public List<Node> Children = new();
            public CompositeNode(string name) : base(name) { }
        }

        [Serializable]
        public class DecoratorNode : Node
        {
            [SerializeReference] public Node Child;
            public DecoratorNode(string name) : base(name) {}

            public async UniTask DefaultExit(CancellationToken _)
            {
                while (!Child.Exit())
                    await UniTask.Yield();
            }
        }
    }
}

#endif
