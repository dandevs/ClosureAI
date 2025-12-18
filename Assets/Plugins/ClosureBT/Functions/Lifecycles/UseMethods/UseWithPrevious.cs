#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that emits a tuple containing the previous and current values from a source variable.
        /// Each emission contains the last two values as a (Previous, Current) tuple.
        /// The first signal is skipped since there's no previous value yet; emissions start from the second signal onwards.
        /// </summary>
        /// <typeparam name="T">The type of values from the source variable.</typeparam>
        /// <param name="source">The source variable to monitor.</param>
        /// <returns>A variable containing a tuple of (previous, current) values.</returns>
        public static VariableType<(T Previous, T Current)> UseWithPrevious<T>(VariableType<T> source)
        {
            var result = Variable<(T Previous, T Current)>();
            var _hasValue = Variable(static () => false);
            var _previousValue = Variable<T>(static () => default);

            source.OnSignal += value =>
            {
                if (!_hasValue.Value)
                {
                    _hasValue.Value = true;
                    _previousValue.Value = value;
                    return;
                }

                result.Value = (_previousValue.Value, value);
                _previousValue.Value = value;
            };

            return result;
        }

        /// <summary>
        /// Creates a variable that emits a tuple containing the previous and current values from a source variable,
        /// starting from the first signal using a seed value for the initial "previous".
        /// Unlike the seedless overload, this emits immediately on the first signal using the seed as the initial previous value.
        /// </summary>
        /// <typeparam name="T">The type of values from the source variable.</typeparam>
        /// <param name="source">The source variable to monitor.</param>
        /// <param name="seed">The initial value to use as "previous" for the first emission.</param>
        /// <returns>A variable containing a tuple of (previous, current) values.</returns>
        public static VariableType<(T Previous, T Current)> UseWithPrevious<T>(VariableType<T> source, T seed)
        {
            var result = Variable<(T Previous, T Current)>();
            var _previousValue = Variable(() => seed);

            source.OnSignal += value =>
            {
                result.Value = (_previousValue.Value, value);
                _previousValue.Value = value;
            };

            return result;
        }

        /// <summary>
        /// Creates a variable that emits a tuple containing the previous and current values from a source variable,
        /// starting from the first signal using a factory function for the initial "previous".
        /// Similar to RxJS's pairwise operator but emits immediately using the seed factory result as the initial previous value, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="T">The type of values from the source variable.</typeparam>
        /// <param name="source">The source variable to monitor.</param>
        /// <param name="seedFn">A factory function that provides the initial value to use as "previous" for the first emission.</param>
        /// <returns>A variable containing a tuple of (previous, current) values.</returns>
        public static VariableType<(T Previous, T Current)> UseWithPrevious<T>(VariableType<T> source, Func<T> seedFn)
        {
            var result = Variable<(T Previous, T Current)>();
            var _previousValue = Variable(seedFn);

            source.OnSignal += value =>
            {
                result.Value = (_previousValue.Value, value);
                _previousValue.Value = value;
            };

            return result;
        }
    }
}

#endif
