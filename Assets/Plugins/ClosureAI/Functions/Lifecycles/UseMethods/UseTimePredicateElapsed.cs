#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a variable that tracks elapsed time since a predicate first became true.
        /// The timer starts when the predicate becomes true and resets when it becomes false.
        /// </summary>
        /// <param name="predicate">A function that returns true to start timing, false to reset.</param>
        /// <returns>A variable containing the elapsed time in seconds since the predicate became true.</returns>
        public static VariableType<float> UseTimePredicateElapsed(Func<float, bool> predicate)
        {
            var elapsed = Variable(static () => 0f);
            var _timeBegan = Variable(static () => 0.0);
            var _state = Variable(static () => 0);

            OnPreTick(() =>
            {
                switch (_state.Value)
                {
                    case 0:
                    {
                        if (predicate(0f))
                        {
                            _timeBegan.Value = Time.timeAsDouble;
                            _state.Value = 1;
                        }

                        break;
                    }
                    case 1:
                    {
                        var e = (float)(Time.timeAsDouble - _timeBegan.Value);

                        if (!predicate(e))
                        {
                            _state.Value = 0;
                            elapsed.SetValueSilently(0f);
                        }
                        else
                            elapsed.Value = e;

                        break;
                    }
                }
            });

            return elapsed;
        }

        public static VariableType<float> UseTimePredicateElapsed<T>(VariableType<T> source, Func<float, bool> predicate)
        {
            var elapsed = Variable(static () => 0f);
            var _timeBegan = Variable(static () => 0.0);
            var _state = Variable(static () => 0);
            var _receivedInitialSignal = Variable(static () => false);

            OnPreTick(() =>
            {
                switch (_state.Value)
                {
                    case 0:
                    {
                        if (_receivedInitialSignal.Value && predicate(0f))
                        {
                            _timeBegan.Value = Time.timeAsDouble;
                            elapsed.Value = 0f;
                            _state.Value = 1;
                        }

                        break;
                    }

                    case 1:
                    {
                        var e = (float)(Time.timeAsDouble - _timeBegan.Value);

                        if (!predicate(e))
                        {
                            _state.Value = 0;
                            elapsed.SetValueSilently(0f);
                        }
                        else
                            elapsed.Value = e;

                        break;
                    }
                }
            });

            source.OnSignal += _ =>
            {
                if (!_receivedInitialSignal.Value)
                    _receivedInitialSignal.Value = true;
            };

            return elapsed;
        }
    }
}

#endif