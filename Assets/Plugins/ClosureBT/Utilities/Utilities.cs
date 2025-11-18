#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ClosureBT.BT;

namespace ClosureBT.Utilities
{
    public static class Utilities
    {
        public static Vector2 WithX(this Vector2 vector2, float x) => new(x, vector2.y);
        public static Vector2 WithY(this Vector2 vector2, float y) => new(vector2.x, y);

        public static async UniTask ContinueWith<T0, T1>(this UniTask task, T0 binder0, T1 binder1, Action<T0, T1> continuation)
        {
            await task;
            continuation(binder0, binder1);
        }

        public static async UniTask ContinueWith<T0>(this UniTask task, T0 binder, Action<T0> continuation)
        {
            await task;
            continuation(binder);
        }

        public static async UniTask ContinueWith<T0, TR>(this UniTask<TR> task, T0 binder, Action<T0> continuation)
        {
            await task;
            continuation(binder);
        }

        public static bool AnyInvalid(this List<Node> nodes, out int resultInvalidIndex)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].IsInvalid())
                {
                    resultInvalidIndex = i;
                    return true;
                }
            }

            resultInvalidIndex = -1;
            return false;
        }

        public static bool AnyInvalidToIndex(this List<Node> nodes, int index, out int resultInvalidIndex)
        {
            resultInvalidIndex = -1;

            if (index >= nodes.Count)
                return false;

            for (var i = 0; i <= index; i++)
            {
                if (nodes[i].IsInvalid())
                {
                    resultInvalidIndex = i;
                    return true;
                }
            }

            return false;
        }

        // public static bool InvalidateWithSkipStatusChange(this List<Node> nodes, int currentIndex)
        // {
        //     for (var i = 0; i <= currentIndex; i++)
        //     {
        //         if (nodes[i].IsInvalid())
        //         {
        //             for (var j = 0; j < i; j++)
        //                 nodes[j].SkipResetGracefulStatusChange = true;

        //             for (var k = i + 1; k < nodes.Count; k++)
        //                 nodes[k].SkipResetGracefulStatusChange = false;

        //             return true;
        //         }
        //     }

        //     return false;
        // }
    }
}

#endif
