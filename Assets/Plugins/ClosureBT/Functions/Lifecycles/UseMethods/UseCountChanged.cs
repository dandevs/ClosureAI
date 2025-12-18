#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that counts how many times a source variable has signaled since the node started.
        /// The counter increments by 1 each time the source variable signals.
        /// </summary>
        /// <typeparam name="T">The type of value in the source variable.</typeparam>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable containing the count of signals as an integer.</returns>
        public static VariableType<int> UseCountChanged<T>(VariableType<T> source)
        {
            var count = Variable(static () => 0);

            source.OnSignal += value =>
            {
                count.Value++;
            };

            return count;
        }
    }
}

#endif
