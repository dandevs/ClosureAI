#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a variable that tracks whether a VariableType source has signaled a change.
        /// Returns true for one frame when the source signals, then false until the next signal, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="T">The type of value in the source variable.</typeparam>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable that is true when the source has signaled this frame, false otherwise.</returns>
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
                if (_setToChanged.Value)
                {
                    _setToChanged.Value = false;
                    changed.Value = true;
                }
                else if (changed.Value)
                    changed.SetValueSilently(false);
            });

            return changed;
        }
    }
}

#endif