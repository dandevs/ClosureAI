#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that transforms values from a source variable using a selector function.
        /// Each signal from the source is projected through the transformation before being emitted.
        /// </summary>
        /// <typeparam name="TSource">The type of the source values.</typeparam>
        /// <typeparam name="TResult">The type of the transformed values.</typeparam>
        /// <param name="source">The source variable to transform.</param>
        /// <param name="selector">A function to transform each value.</param>
        /// <returns>A variable containing the transformed values.</returns>
        public static VariableType<TResult> UseSelect<TSource, TResult>(VariableType<TSource> source, Func<TSource, TResult> selector)
        {
            var result = Variable<TResult>();

            // React to source changes and transform them
            source.OnSignal += value =>
            {
                result.Value = selector(value);
            };

            return result;
        }
    }
}


#endif
