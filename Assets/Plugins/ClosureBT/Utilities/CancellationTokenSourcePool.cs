#if UNITASK_INSTALLED
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace ClosureBT.Utilities
{
    public static class CancellationTokenSourcePool
    {
        private static readonly Stack<CancellationTokenSource> pool = new();
        private static readonly ConditionalWeakTable<CancellationTokenSource, CancellationTokenSource> disposed = new();

        private static CancellationTokenSource _cancelledCancellationTokenSource;
        public static CancellationToken cancelledToken => _cancelledCancellationTokenSource.Token;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            _cancelledCancellationTokenSource = new();
            _cancelledCancellationTokenSource.Cancel();
            pool.Clear();
            disposed.Clear();
        }

        public static CancellationTokenSource Get()
        {
            return pool.TryPop(out var cts) ? cts : new();
        }

        public static bool TryReturn(CancellationTokenSource cts)
        {
            if (cts == null)
                return false;

            if (!disposed.TryGetValue(cts, out _) && cts.IsCancellationRequested)
            {
                disposed.Add(cts, cts);
                cts.Dispose();
                return false;
            }

            pool.Push(cts);
            return true;
        }
    }
}

#endif
