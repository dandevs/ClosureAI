#if UNITASK_INSTALLED
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClosureBT
{
    public static partial class BT
    {
        public static Node FromAsync(Func<CancellationToken, UniTask> fn, Action lifecycle = null) => Leaf("From Async", () =>
        {
            OnBaseTick(async (ct, tick) =>
            {
                await fn(ct);
                return Status.Success;
            });

            lifecycle?.Invoke();
        });
    }
}

#endif
