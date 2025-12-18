#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that throttles signals from a source variable, only propagating at most once per specified delay period.
        /// Useful for limiting the frequency of updates when the source signals rapidly.
        /// </summary>
        /// <typeparam name="T">The type of value to throttle.</typeparam>
        /// <param name="delay">The minimum delay in seconds between updates.</param>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable containing the throttled value.</returns>
        public static VariableType<T> UseThrottle<T>(float delay, VariableType<T> source)
        {
            var throttled = Variable(source.Fn);
            var _lastUpdateTime = Variable(static () => 0.0);

            OnPreTick(() =>
            {
                var now = Time.timeAsDouble;

                if (now - _lastUpdateTime.Value >= delay)
                {
                    _lastUpdateTime.Value = now;
                    throttled.Value = source.Value;
                }
            });

            return throttled;
        }
    }
}

#endif
