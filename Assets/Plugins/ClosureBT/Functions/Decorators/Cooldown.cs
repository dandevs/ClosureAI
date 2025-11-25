#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that enforces a cooldown period on its child node.
            /// After the child completes, it cannot execute again until the cooldown duration has elapsed.
            /// During the cooldown period, the decorator returns Failure without ticking the child.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="duration">A function that returns the cooldown duration in seconds</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that enforces a cooldown period on its child</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Tracks elapsed time since last child completion using Time.timeAsDouble</item>
            /// <item>Returns <see cref="Status.Failure"/> immediately if still on cooldown (doesn't tick child)</item>
            /// <item>Ticks the child when cooldown has passed</item>
            /// <item>When child completes, resets elapsed time to 0 (starts new cooldown)</item>
            /// <item>Returns child's status when child completes (after cooldown passed)</item>
            /// <item>Returns <see cref="Status.Running"/> while child is executing</item>
            /// <item>Re-evaluates duration function each tick (allows dynamic cooldowns)</item>
            /// <item>Exits the child when decorator exits</item>
            /// <item>Invalidates when cooldown passed AND child is invalid</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Ability cooldowns (e.g., "can only attack every 2 seconds")</item>
            /// <item>Rate limiting (e.g., "spawn enemy every 5 seconds")</item>
            /// <item>Preventing rapid re-execution of expensive operations</item>
            /// <item>Time-gated behaviors (e.g., "check for updates every 10 seconds")</item>
            /// <item>Animation/audio spacing (e.g., "play sound effect max once per second")</item>
            /// </list>
            ///
            /// <para><b>Example - Attack Cooldown:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     D.Cooldown(2f); // 2 second cooldown
            ///     Sequence("Attack", () =>
            ///     {
            ///         Condition(() => enemyInRange);
            ///         Do(() => PerformAttack());
            ///     });
            ///     // Can only attack every 2 seconds
            ///     // During cooldown, selector tries next option
            ///
            ///     Idle();
            /// });
            /// </code>
            ///
            /// <para><b>Example - Spawn Rate Limiting:</b></para>
            /// <code>
            /// D.Cooldown(5f);
            /// D.Repeat();
            /// Do("Spawn Enemy", () => SpawnEnemy());
            /// // Spawns an enemy every 5 seconds indefinitely
            /// </code>
            ///
            /// <para><b>Example - Dynamic Cooldown:</b></para>
            /// <code>
            /// D.Cooldown(() => currentDifficulty * 2f); // Cooldown scales with difficulty
            /// UseSpecialAbility();
            /// // Harder difficulty = longer cooldown
            /// </code>
            ///
            /// <para><b>Example - Periodic Check:</b></para>
            /// <code>
            /// Parallel(() =>
            /// {
            ///     MainBehavior();
            ///
            ///     D.Cooldown(10f);
            ///     D.Repeat();
            ///     Do("Periodic Update", () => CheckForUpdates());
            ///     // Checks for updates every 10 seconds while main behavior runs
            /// });
            /// </code>
            ///
            /// <para><b>Cooldown Timing:</b></para>
            /// <list type="bullet">
            /// <item>Cooldown starts when child completes (Success or Failure)</item>
            /// <item>Elapsed time is tracked using Time.timeAsDouble for accuracy</item>
            /// <item>Elapsed time resets to 0 on OnEnter and OnDeserialize</item>
            /// <item>Time tracking pauses when tree is not ticking</item>
            /// </list>
            ///
            /// <para><b>Failure During Cooldown:</b></para>
            /// When on cooldown, returns Failure immediately without ticking child.
            /// This allows parent selectors to try alternative behaviors during cooldown.
            ///
            /// <para><b>Invalidation Logic:</b></para>
            /// OnInvalidCheck updates elapsed time and returns true only if:
            /// <list type="bullet">
            /// <item>Cooldown has passed (elapsed >= duration)</item>
            /// <item>AND child is invalid</item>
            /// </list>
            /// This allows reactive parents to respond when the cooldown expires and child needs re-execution.
            ///
            /// <para><b>Difference from D.Timeout:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Cooldown:</b> Prevents execution for duration AFTER completion</item>
            /// <item><b>D.Timeout:</b> Limits execution time, fails if child takes too long</item>
            /// </list>
            ///
            /// <para><b>Difference from Wait:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Cooldown:</b> Prevents child from running for duration after it completes</item>
            /// <item><b>Wait:</b> Delays for duration then succeeds (no child)</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Cooldown(3f);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Cooldown(string name, Func<float> duration, Action setup = null) => Decorator("Cooldown", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                var _duration = Variable(0f, duration);
                var _elapsed = Variable(9999999f);
                var timeLastTick = Time.timeAsDouble;

                SetNodeName(name);

                OnInvalidCheck(() =>
                {
                    _elapsed.SetValueSilently(_elapsed.Value + (float)(Time.timeAsDouble - timeLastTick));
                    timeLastTick = Time.timeAsDouble;
                    return _elapsed.Value >= _duration.Value && node.Child.IsInvalid();
                });

                OnExit(_ =>
                {
                    timeLastTick = Time.timeAsDouble;
                    return ExitNode(node.Child);
                });

                OnEnter(() => timeLastTick = Time.timeAsDouble);
                OnDeserialize(() => timeLastTick = Time.timeAsDouble);

                OnBaseTick(() =>
                {
                    _duration.Value = duration();

                    // Check if still on cooldown using _elapsed
                    if (_elapsed.Value < _duration.Value)
                    {
                        return Status.Failure;
                    }

                    // Cooldown has passed, tick the child
                    if (node.Child.Tick(out var status, true))
                    {
                        // Child completed, reset elapsed time for next cooldown
                        _elapsed.SetValueSilently(0f);
                        return status;
                    }

                    return Status.Running;
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
            public static DecoratorNode Cooldown(Func<float> duration, Action setup = null)
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
            public static DecoratorNode Cooldown(string name, float duration, Action setup = null)
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
            public static DecoratorNode Cooldown(float duration, Action setup = null)
            {
                return Cooldown("Cooldown", () => duration, setup);
            }
        }
    }
}

#endif
