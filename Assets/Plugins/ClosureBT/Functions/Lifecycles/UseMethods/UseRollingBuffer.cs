#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that maintains a rolling buffer of values with a specified maximum count.
        /// When the buffer exceeds the count, the oldest values are removed.
        /// </summary>
        /// <typeparam name="T">The type of values to store in the buffer.</typeparam>
        /// <param name="count">The maximum number of values to keep in the buffer.</param>
        /// <param name="waitToFillBufferFirst">If true, waits for the buffer to reach full capacity before emitting signals.</param>
        /// <param name="source">The source variable to monitor for changes.</param>
        /// <returns>A variable containing a list of buffered values.</returns>
        public static VariableType<List<T>> UseRollingBuffer<T>(int count, bool waitToFillBufferFirst, VariableType<T> source)
        {
            count = Mathf.Max(1, count);
            var buffer = Variable(new List<T>(count));

            buffer.OnInitialize(() => buffer.Value.Clear());

            source.OnSignal += value =>
            {
                var list = buffer.Value;
                list.Add(value);

                while (list.Count > count)
                    list.RemoveAt(0);

                if (waitToFillBufferFirst && list.Count < count)
                    return;

                buffer.Value = buffer.Value;
            };

            return buffer;
        }
    }
}

#endif
