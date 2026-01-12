#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that only emits when the value from a source variable actually changes.
        /// Compares consecutive values using the default equality comparer and only propagates signals when they differ,
        /// preventing duplicate updates. Useful for preventing redundant state transitions and avoiding unnecessary processing.
        /// </summary>
        /// <typeparam name="T">The type of value to monitor for changes.</typeparam>
        /// <param name="source">The source variable to monitor for distinct changes.</param>
        /// <returns>A variable that only emits when the value actually changes.</returns>
        /// <example>
        /// <code>
        /// var health = Variable(() => 100);
        /// var distinctHealth = UseDistinctUntilChanged(health);
        /// // distinctHealth only updates when health value actually changes
        /// </code>
        /// </example>
        public static VariableType<T> UseDistinctUntilChanged<T>(VariableType<T> source)
        {
            return UseDistinctUntilChanged(source, null);
        }

        /// <summary>
        /// Creates a variable that only emits when the value from a source variable actually changes.
        /// Compares consecutive values using a custom equality comparer and only propagates signals when they differ,
        /// preventing duplicate updates. Useful for preventing redundant state transitions and avoiding unnecessary processing.
        /// </summary>
        /// <typeparam name="T">The type of value to monitor for changes.</typeparam>
        /// <param name="source">The source variable to monitor for distinct changes.</param>
        /// <param name="equalityComparer">Custom equality comparer function. If null, uses default equality.</param>
        /// <returns>A variable that only emits when the value actually changes.</returns>
        /// <example>
        /// <code>
        /// var position = Variable(() => Vector3.zero);
        /// var distinctPos = UseDistinctUntilChanged(position, (a, b) => Vector3.Distance(a, b) &lt; 0.01f);
        /// // Only updates when position changes by more than 0.01 units
        /// </code>
        /// </example>
        public static VariableType<T> UseDistinctUntilChanged<T>(VariableType<T> source, Func<T, T, bool> equalityComparer)
        {
            var result = Variable(source.Fn);
            var _hasValue = Variable(static () => false);
            var _previousValue = Variable<T>();

            var comparer = equalityComparer ?? EqualityComparer<T>.Default.Equals;

            source.OnSignal += value =>
            {
                if (!_hasValue.Value)
                {
                    _hasValue.Value = true;
                    _previousValue.Value = value;
                    result.Value = value;
                }
                else if (!comparer(_previousValue.Value, value))
                {
                    _previousValue.Value = value;
                    result.Value = value;
                }
                // If values are equal, don't propagate the signal
            };

            return result;
        }
    }
}


#endif
