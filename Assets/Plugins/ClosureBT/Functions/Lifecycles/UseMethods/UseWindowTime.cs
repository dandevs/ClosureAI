#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that maintains a sliding time window of values from a source variable.
        /// Collects all signals that occurred within the specified time duration, automatically removing old values.
        /// Useful for tracking recent events like "damage taken in last 3 seconds" or "inputs in last 0.5 seconds".
        /// </summary>
        /// <typeparam name="T">The type of values to collect in the window.</typeparam>
        /// <param name="windowDuration">The duration in seconds to keep values in the window.</param>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable containing a list of values within the time window.</returns>
        public static VariableType<List<T>> UseWindowTime<T>(float windowDuration, VariableType<T> source)
        {
            var window = Variable(new List<T>());
            var _timestamps = Variable(new List<double>());

            window.OnInitialize(() =>
            {
                window.Value.Clear();
                _timestamps.Value.Clear();
            });

            source.OnSignal += value =>
            {
                var currentTime = Time.timeAsDouble;
                var cutoffTime = currentTime - windowDuration;

                // Add new value
                window.Value.Add(value);
                _timestamps.Value.Add(currentTime);

                // Remove expired values
                var windowList = window.Value;
                var timeList = _timestamps.Value;

                while (timeList.Count > 0 && timeList[0] < cutoffTime)
                {
                    windowList.RemoveAt(0);
                    timeList.RemoveAt(0);
                }

                // Trigger signal with updated window
                window.Value = window.Value;
            };

            // Clean up expired values each tick (in case no new signals arrive)
            OnPreTick(() =>
            {
                if (_timestamps.Value.Count == 0)
                    return;

                var currentTime = Time.timeAsDouble;
                var cutoffTime = currentTime - windowDuration;
                var windowList = window.Value;
                var timeList = _timestamps.Value;
                var removedAny = false;

                while (timeList.Count > 0 && timeList[0] < cutoffTime)
                {
                    windowList.RemoveAt(0);
                    timeList.RemoveAt(0);
                    removedAny = true;
                }

                if (removedAny)
                    window.Value = window.Value; // Trigger signal if we removed items
            });

            return window;
        }

        /// <summary>
        /// Creates a variable that maintains a sliding time window of timestamped values from a source variable.
        /// Each entry includes both the value and the timestamp when it was received.
        /// Useful when you need to know both what happened and when it happened within the time window.
        /// </summary>
        /// <typeparam name="T">The type of values to collect in the window.</typeparam>
        /// <param name="windowDuration">The duration in seconds to keep values in the window.</param>
        /// <param name="source">The source variable to monitor for signals.</param>
        /// <returns>A variable containing a list of tuples with values and their timestamps.</returns>
        public static VariableType<List<(T value, double timestamp)>> UseWindowTimeWithTimestamps<T>(
            float windowDuration,
            VariableType<T> source)
        {
            var window = Variable(new List<(T, double)>());

            window.OnInitialize(() => window.Value.Clear());

            source.OnSignal += value =>
            {
                var currentTime = Time.timeAsDouble;
                var cutoffTime = currentTime - windowDuration;

                // Add new value with timestamp
                window.Value.Add((value, currentTime));

                // Remove expired values
                var windowList = window.Value;

                while (windowList.Count > 0 && windowList[0].Item2 < cutoffTime)
                    windowList.RemoveAt(0);

                // Trigger signal with updated window
                window.Value = window.Value;
            };

            // Clean up expired values each tick
            OnPreTick(() =>
            {
                if (window.Value.Count == 0)
                    return;

                var currentTime = Time.timeAsDouble;
                var cutoffTime = currentTime - windowDuration;
                var windowList = window.Value;
                var removedAny = false;

                while (windowList.Count > 0 && windowList[0].Item2 < cutoffTime)
                {
                    windowList.RemoveAt(0);
                    removedAny = true;
                }

                if (removedAny)
                    window.Value = window.Value; // Trigger signal if we removed items
            });

            return window;
        }
    }
}

#endif
