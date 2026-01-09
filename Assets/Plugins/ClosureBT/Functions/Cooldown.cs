#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
            public static LeafNode Cooldown(string name, Func<float> duration, Action setup = null) => Leaf("Cooldown", () =>
            {
                var node = (LeafNode)CurrentNode;
                var _elapsed = Variable(99999f);
                var timeLastTick = Time.timeAsDouble;

                SetNodeName(name);

                OnInvalidCheck(() =>
                {
                    _elapsed.SetValueSilently(_elapsed.Value + (float)(Time.timeAsDouble - timeLastTick));
                    timeLastTick = Time.timeAsDouble;
                    return _elapsed.Value >= duration();
                });

                OnExit(() => timeLastTick = Time.timeAsDouble);
                OnEnter(() => timeLastTick = Time.timeAsDouble);
                OnDeserialize(() => timeLastTick = Time.timeAsDouble);

                OnBaseTick(() =>
                {
                    _elapsed.Value += (float)(Time.timeAsDouble - timeLastTick);
                    timeLastTick = Time.timeAsDouble;

                    if (_elapsed.Value < duration())
                        return Status.Failure;
                    else
                    {
                        _elapsed.SetValueSilently(0f);
                        return Status.Success;
                    }
                });

                setup?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that enforces a cooldown period on its child node.
            /// After the child completes, it cannot execute again until the cooldown duration has elapsed.
            /// During the cooldown period, the decorator returns Failure without ticking the child.
            /// </summary>
            /// <param name="duration">A function that returns the cooldown duration in seconds</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a cooldown period on its child</returns>
            /// <remarks>
            /// See <see cref="Cooldown(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses an auto-generated name.
            /// </remarks>
            public static LeafNode Cooldown(Func<float> duration, Action setup = null)
            {
                return Cooldown("Cooldown", duration, setup);
            }

            /// <summary>
            /// Creates a decorator that enforces a cooldown period on its child node.
            /// After the child completes, it cannot execute again until the cooldown duration has elapsed.
            /// During the cooldown period, the decorator returns Failure without ticking the child.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="duration">The cooldown duration in seconds (constant value)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a cooldown period on its child</returns>
            /// <remarks>
            /// See <see cref="Cooldown(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses a constant duration.
            /// </remarks>
            public static LeafNode Cooldown(string name, float duration, Action setup = null)
            {
                return Cooldown(name, () => duration, setup);
            }

            /// <summary>
            /// Creates a decorator that enforces a cooldown period on its child node.
            /// After the child completes, it cannot execute again until the cooldown duration has elapsed.
            /// During the cooldown period, the decorator returns Failure without ticking the child.
            /// </summary>
            /// <param name="duration">The cooldown duration in seconds (constant value)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a cooldown period on its child</returns>
            /// <remarks>
            /// See <see cref="Cooldown(string, Func{float}, Action)"/> for full documentation.
            /// This overload uses a constant duration and auto-generated name.
            /// </remarks>
            public static LeafNode Cooldown(float duration, Action setup = null)
            {
                return Cooldown("Cooldown", () => duration, setup);
            }
    }
}
#endif
