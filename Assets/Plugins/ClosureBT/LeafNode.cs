#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static LeafNode Leaf(string name, Action setup)
        {
            var parentNode = CurrentNode;

            var node = new LeafNode("Leaf")
            {
                IsReactive = parentNode?.IsReactive ?? false,
                Name = name,
                Parent = parentNode,
            };

            if (_decorators.TryPop(out var decorator))
            {
                decorator.Child = node;
            }
            else
            {
                if (parentNode is CompositeNode composite)
                    composite.Children.Add(node);
            }

            // CurrentTree.Nodes.Add(node);
            _nodeStack.Push(node);
            setup();
            _nodeStack.Pop();

            return node;
        }

        public static LeafNode Leaf(Action setup) => Leaf("Leaf", setup);
    }
}

#endif
