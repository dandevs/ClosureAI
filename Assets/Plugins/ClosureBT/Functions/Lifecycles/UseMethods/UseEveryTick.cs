#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that samples the source variable's value every tick.
        /// The output signals on every tick, regardless of whether the source changed.
        /// </summary>
        /// <typeparam name="T">The type of value in the source variable.</typeparam>
        /// <param name="source">The source variable to sample each tick.</param>
        /// <returns>A variable that signals every tick with the source's current value.</returns>
        public static VariableType<T> UseEveryTick<T>(VariableType<T> source)
        {
            var variable = Variable(source.Fn);

            OnPreTick(() =>
            {
                variable.Value = source.Value;
            });

            return variable;
        }

        /// <summary>
        /// Creates a variable that evaluates a function every tick and signals with the result.
        /// The output signals on every tick with the function's return value.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the function.</typeparam>
        /// <param name="source">A function to evaluate each tick.</param>
        /// <returns>A variable that signals every tick with the function's return value.</returns>
        public static VariableType<T> UseEveryTick<T>(Func<T> source)
        {
            var variable = Variable<T>();

            variable.OnInitialize(() => variable.Value = source());

            OnPreTick(() =>
            {
                variable.Value = source();
            });

            return variable;
        }
    }
}


#endif
