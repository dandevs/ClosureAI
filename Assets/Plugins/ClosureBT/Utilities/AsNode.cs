#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
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
