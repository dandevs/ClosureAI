#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that tracks whether a source variable has signaled a value change.
        /// Returns true for one tick when the source signals a new value, then resets to false until the next signal.
        /// </summary>
        /// <typeparam name="T">The type of value in the source variable.</typeparam>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable that is true when the source has signaled this tick, false otherwise.</returns>
        public static VariableType<bool> UseValueDidChange<T>(VariableType<T> source)
        {
            var changed = Variable(static () => false);
            var _previous = Variable(source.Fn);
            var _setToChanged = Variable(false);
            Func<T, T, bool> equals = EqualityComparer<T>.Default.Equals;

            source.OnSignal += value =>
            {
                if (!equals(_previous.Value, value))
                {
                    _previous.Value = value;
                    _setToChanged.Value = true;
                }
            };

            OnExit(() => _setToChanged.Value = false);

            OnPreTick(() =>
            {
                if (changed.Value)
                    changed.SetValueSilently(false);

                if (_setToChanged.Value)
                {
                    _setToChanged.Value = false;
                    changed.Value = true;
                }
            });

            return changed;
        }
    }
}

#endif
