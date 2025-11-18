#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that filters values from a source variable using a predicate function.
        /// Similar to LINQ's Where, this only propagates values that satisfy the condition, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="T">The type of values to filter.</typeparam>
        /// <param name="source">The source variable to filter.</param>
        /// <param name="predicate">A function to test each value for a condition.</param>
        /// <returns>A variable that only updates when the predicate returns true.</returns>
        public static VariableType<T> UseWhere<T>(Func<T, bool> predicate, VariableType<T> source)
        {
            var result = Variable(source.Fn);

            // Only propagate values that pass the predicate
            source.OnSignal += value =>
            {
                if (predicate(value))
                    result.Value = value;
            };

            return result;
        }
    }
}

#endif
