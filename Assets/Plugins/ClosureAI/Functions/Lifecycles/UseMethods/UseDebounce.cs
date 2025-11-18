#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that debounces signals from a VariableType source, only propagating after the source stops changing for the specified delay period.
        /// Useful for waiting until rapid changes settle before reacting, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="T">The type of value to debounce.</typeparam>
        /// <param name="delay">The delay in seconds to wait after the last change before updating.</param>
        /// <param name="source">The source variable to monitor for changes.</param>
        /// <returns>A variable containing the debounced value.</returns>
        public static VariableType<T> UseDebounce<T>(float delay, VariableType<T> source)
        {
            var debounced = Variable(source.Fn);
            var _lastChangeTime = Variable(static () => 0.0);
            var _pendingValue = Variable<T>();
            var _hasPendingValue = Variable(static () => false);

            // Track when source value changes
            source.OnSignal += value =>
            {
                _lastChangeTime.Value = Time.timeAsDouble;
                _pendingValue.Value = value;
                _hasPendingValue.Value = true;
            };

            OnPreTick(() =>
            {
                if (_hasPendingValue.Value)
                {
                    var now = Time.timeAsDouble;

                    // Only update if enough time has passed since the last change
                    if (now - _lastChangeTime.Value >= delay)
                    {
                        debounced.Value = _pendingValue.Value;
                        _hasPendingValue.Value = false;
                    }
                }
            });

            return debounced;
        }
    }
}

#endif
