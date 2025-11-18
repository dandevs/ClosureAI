#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        public static DecoratorNode Decorator(string name, Action setup) {
            var parentNode = CurrentNode;
            var node = new DecoratorNode("Decorator")
            {
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
            _decorators.Push(node);
            _nodeStack.Push(node);
            setup();
            _nodeStack.Pop();

            return node;
        }

        public static DecoratorNode Decorator(Action setup) => Decorator("Decorator", setup);
    }
}

#endif
