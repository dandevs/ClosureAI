#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a variable that transforms values from a source variable using a selector function.
        /// Similar to LINQ's Select, this projects each value through a transformation, enabling FRP-style reactive programming.
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