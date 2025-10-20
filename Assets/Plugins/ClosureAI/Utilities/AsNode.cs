#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureAI
{
    public static partial class AI
    {
        public static LeafNode AsNode(Func<CancellationToken, UniTask> func, Action setup = null) => Leaf(
            func.Method.Name, () =>
            {
                OnEnter(func);
                setup?.Invoke();
            });
    }
}

#endif