#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that accumulates values over time using an accumulator function.
        /// Similar to Array.Reduce but for reactive streams - each new value is combined with the accumulated result.
        /// Useful for tracking cumulative totals, combo counters, or any stateful aggregation, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="TSource">The type of the source values.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulated result.</typeparam>
        /// <param name="source">The source variable providing values to accumulate.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">A function to combine the current accumulation with each new value.</param>
        /// <returns>A variable containing the accumulated result.</returns>
        public static VariableType<TAccumulate> UseScan<TSource, TAccumulate>(
            VariableType<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            var result = Variable(() => seed);

            source.OnSignal += value =>
            {
                result.Value = accumulator(result.Value, value);
            };

            return result;
        }

        /// <summary>
        /// Creates a variable that accumulates values over time using an accumulator function with index tracking.
        /// Similar to Array.Reduce but includes the signal index in the accumulator function, enabling FRP-style reactive programming.
        /// </summary>
        /// <typeparam name="TSource">The type of the source values.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulated result.</typeparam>
        /// <param name="source">The source variable providing values to accumulate.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="accumulator">A function to combine the current accumulation with each new value and index.</param>
        /// <returns>A variable containing the accumulated result.</returns>
        public static VariableType<TAccumulate> UseScan<TSource, TAccumulate>(
            VariableType<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, int, TAccumulate> accumulator)
        {
            var result = Variable(() => seed);
            var _index = Variable(static () => 0);

            source.OnSignal += value =>
            {
                result.Value = accumulator(result.Value, value, _index.Value);
                _index.Value++;
            };

            return result;
        }
    }
}


#endif
