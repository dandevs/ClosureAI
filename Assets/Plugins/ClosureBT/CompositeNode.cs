#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static CompositeNode Composite(string name, Action setup)
        {
            var parentNode = CurrentNode;
            var node = new CompositeNode("Composite")
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
            _nodeStack.Push(node);
            setup();
            _nodeStack.Pop();

            return node;
        }

        public static T Composite<T>(string name, Action setup) where T : CompositeNode, new()
        {
            var parentNode = CurrentNode;
            var node = new T()
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
            _nodeStack.Push(node);
            setup();
            _nodeStack.Pop();

            return node;
        }

        public static CompositeNode Composite(Action setup) => Composite("Composite", setup);
    }
}

#endif
