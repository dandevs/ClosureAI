#if UNITASK_INSTALLED
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a variable that tracks elapsed time since the node started.
        /// The elapsed time accumulates continuously while the node is active.
        /// </summary>
        /// <returns>A variable containing the elapsed time in seconds as a float.</returns>
        public static VariableType<float> UseTimeElapsed()
        {
            var elapsed = Variable(static () => 0f);
            var _lastTime = Variable(static () => Time.timeAsDouble);

            OnEnter(() => _lastTime.Value = Time.timeAsDouble);
            OnDeserialize(() => _lastTime.Value = Time.timeAsDouble);

            OnPreTick(() =>
            {
                var now = Time.timeAsDouble;
                elapsed.Value += (float)(now - _lastTime.Value);
                _lastTime.Value = now;
            });

            return elapsed;
        }
    }
}

#endif
