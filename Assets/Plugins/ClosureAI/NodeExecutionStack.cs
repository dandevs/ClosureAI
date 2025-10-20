#if UNITASK_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI
{
    [Serializable]
    public class NodeExecutionStack : IEnumerable<Node>
    {
        [SerializeReference] public List<List<Node>> stack = new();
        private int hashCode;

        public void Add(List<Node> nodeList)
        {
            stack.Add(nodeList);
        }

        public void Add(Node node)
        {
            if (stack.Count == 0)
                stack.Add(ListPool.Get<Node>());

            stack[^1].Add(node);
        }

        public Node Pop()
        {
            if (stack.Count == 0)
                throw new InvalidOperationException("Stack is empty");

            var list = stack[^1];

            if (list.Count > 0)
            {
                var node = list[^1];
                list.RemoveAt(list.Count - 1);

                if (list.Count == 0)
                    stack.RemoveAt(stack.Count - 1);

                return node;
            }

            throw new InvalidOperationException("Stack is empty");
        }

        public Node Peek()
        {
            if (stack.Count > 0)
                return stack[^1][^1];
            else
                throw new InvalidOperationException("Stack is empty");
        }

        public bool TryPeek(out Node node)
        {
            if (stack.Count > 0)
            {
                node = stack[^1][^1];
                return true;
            }
            else
            {
                node = null;
                return false;
            }
        }

        public void CreateNewStack()
        {
            stack.Add(new());
        }

        public IEnumerator<Node> GetEnumerator() => new ExecutionStackEnumerator(stack);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct ExecutionStackEnumerator : IEnumerator<Node>
        {
            private readonly List<List<Node>> stack;
            private int outerIndex;
            private int innerIndex;

            public ExecutionStackEnumerator(List<List<Node>> stack)
            {
                this.stack = stack;
                outerIndex = 0;
                innerIndex = -1;
                Current = null;
            }

            public bool MoveNext()
            {
                if (stack.Count == 0) return false;

                innerIndex++;
                while (outerIndex < stack.Count)
                {
                    if (innerIndex < stack[outerIndex].Count)
                    {
                        Current = stack[outerIndex][innerIndex];
                        return true;
                    }

                    outerIndex++;
                    innerIndex = 0;
                }

                return false;
            }

            public void Reset()
            {
                outerIndex = 0;
                innerIndex = -1;
                Current = null;
            }

            public Node Current { get; private set; }
            object IEnumerator.Current => Current;

            public void Dispose() { }
        }

        public Node this[int index]
        {
            get
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var currentIndex = 0;

                foreach (var list in stack)
                {
                    if (index < currentIndex + list.Count)
                        return list[index - currentIndex];

                    currentIndex += list.Count;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            var currentIndex = 0;

            for (var i = 0; i < stack.Count; i++)
            {
                var list = stack[i];

                if (index < currentIndex + list.Count)
                {
                    list.RemoveAt(index - currentIndex);

                    // Remove the empty list from stack if needed
                    if (list.Count == 0)
                        stack.RemoveAt(i);

                    return;
                }

                currentIndex += list.Count;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        // The total length of all lists in the stack
        public int Count
        {
            get
            {
                var count = 0;

                foreach (var list in stack)
                    count += list.Count;

                return count;
            }
        }

        // public void PopTo(AI.Node nodeToPopTo, Action<AI.Node, Action> action) {
        //     var i = 0;
        //
        //
        // }

        public void Clear()
        {
            foreach (var list in stack)
                ListPool.Return(list);

            stack.Clear();
        }

        // public static int GetStackHashCode() {
        //
        // }
    }

    internal static class ListPool
    {
        public static List<T> Get<T>()
        {
            return Pool<T>.Get();
        }

        public static void Return<T>(List<T> list)
        {
            Pool<T>.Return(list);
        }

        private static class Pool<T>
        {
            private static readonly Stack<List<T>> pool = new();

            public static List<T> Get()
            {
                return pool.TryPop(out var list) ? list : new();
            }

            public static void Return(List<T> list)
            {
                list.Clear();
                pool.Push(list);
            }
        }
    }
}

#endif