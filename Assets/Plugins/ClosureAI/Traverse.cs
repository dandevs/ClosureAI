#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        public delegate void RefAction<T>(ref T binder, Node node);

        public partial class Node
        {
            public static void Traverse<T>(T binder, Node node, Func<Node, bool> predicate, Action<T, Node> onTraverse)
            {
                if (!predicate(node))
                    return;

                onTraverse(binder, node);

                if (node is CompositeNode composite)
                {
                    foreach (var child in composite.Children)
                        Traverse(binder, child, predicate, onTraverse);
                }

                else if (node is DecoratorNode decorator)
                    Traverse(binder, decorator.Child, predicate, onTraverse);
            }

            public static void Traverse(Node node, Func<Node, bool> predicate, Action<Node> onTraverse)
            {
                Traverse(onTraverse, node, predicate, static (f, n) => f(n));
            }

            public static void Traverse(Node node, Action<Node> onTraverse)
            {
                Traverse(node, static _ => true, onTraverse);
            }

            //***************************************************************************************************************

            public static void TraverseDepthFirst<T>(T binder, Node node, Func<Node, bool> predicate, Action<T, Node> onTraverse)
            {
                if (!predicate(node))
                    return;

                if (node is CompositeNode composite)
                {
                    for (var i = composite.Children.Count - 1; i >= 0; i--)
                        TraverseDepthFirst(binder, composite.Children[i], predicate, onTraverse);
                }
                else if (node is DecoratorNode decorator)
                    TraverseDepthFirst(binder, decorator.Child, predicate, onTraverse);

                onTraverse(binder, node);
            }

            public static void TraverseDepthFirst(Node node, Func<Node, bool> predicate, Action<Node> onTraverse)
            {
                TraverseDepthFirst(onTraverse, node, predicate, static (f, n) => f(n));
            }

            public static void TraverseDepthFirst(Node node, Action<Node> onTraverse)
            {
                TraverseDepthFirst(node, static _ => true, onTraverse);
            }
        }
    }
}

#endif