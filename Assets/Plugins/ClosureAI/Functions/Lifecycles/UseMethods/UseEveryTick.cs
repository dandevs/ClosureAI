#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        public static VariableType<T> UseEveryTick<T>(VariableType<T> source)
        {
            var variable = Variable(source.Fn);

            OnPreTick(() =>
            {
                variable.Value = source.Value;
            });

            return variable;
        }

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