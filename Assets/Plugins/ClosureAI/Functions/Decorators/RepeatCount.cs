#if UNITASK_INSTALLED
using System;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that repeats its child node a specified number of times.
            /// Returns Success after the child has completed the specified number of iterations.
            /// </summary>
            /// <param name="amount">A function that returns the number of times to repeat the child</param>
            /// <param name="lifecycle">Optional lifecycle callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that repeats its child a fixed number of times</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Tracks the current iteration count in a variable</item>
            /// <item>Ticks the child with allowReEnter=true when it's not Done</item>
            /// <item>When child completes, increments the iteration counter</item>
            /// <item>Returns <see cref="Status.Success"/> when iteration count reaches the specified amount</item>
            /// <item>Returns <see cref="Status.Running"/> while still iterating</item>
            /// <item>Resets the child via ResetGracefully() after each completion (for next iteration)</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Fixed-count loops (e.g., "attack 3 times")</item>
            /// <item>Batch processing (e.g., "collect 5 resources")</item>
            /// <item>Training sequences (e.g., "practice maneuver 10 times")</item>
            /// <item>Timed repetitions (e.g., "flash warning 3 times")</item>
            /// <item>Limited retry attempts</item>
            /// </list>
            ///
            /// <para><b>Example - Attack Combo:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.RepeatCount(3);
            ///     Sequence("Single Attack", () =>
            ///     {
            ///         Do(() => PerformAttack());
            ///         Wait(0.3f);
            ///     });
            ///
            ///     Do(() => FinishCombo());
            /// });
            /// // Performs attack 3 times with 0.3s delay, then finishes combo
            /// </code>
            ///
            /// <para><b>Example - Resource Collection:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.RepeatCount(() => resourcesNeeded);
            ///     Sequence("Gather One", () =>
            ///     {
            ///         MoveTo(() => FindNearestResource());
            ///         Do(() => Collect());
            ///     });
            ///
            ///     Do(() => ReturnToBase());
            /// });
            /// // Collects exactly resourcesNeeded items, then returns to base
            /// </code>
            ///
            /// <para><b>Example - Retry Mechanism:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     D.RepeatCount(3);
            ///     Sequence("Attempt", () =>
            ///     {
            ///         Condition(() => AttemptConnection());
            ///         Do(() => OnConnectionSuccess());
            ///     });
            ///
            ///     Do(() => OnAllAttemptsFailed());
            /// });
            /// // Tries to connect 3 times, runs failure handler if all attempts fail
            /// </code>
            ///
            /// <para><b>Example - Dynamic Count:</b></para>
            /// <code>
            /// var level = 1;
            ///
            /// D.RepeatCount(() => level * 2); // Count updates based on current level
            /// SpawnEnemy();
            /// // Spawns 2 enemies at level 1, 4 at level 2, 6 at level 3, etc.
            /// </code>
            ///
            /// <para><b>Iteration Counting:</b></para>
            /// <list type="bullet">
            /// <item>Counter starts at 0 when decorator enters</item>
            /// <item>Counter increments each time child completes (regardless of Success/Failure)</item>
            /// <item>Counter resets to 0 when decorator is reset</item>
            /// <item>Amount function is re-evaluated each tick (allows dynamic counts)</item>
            /// </list>
            ///
            /// <para><b>Child Reset Behavior:</b></para>
            /// After each completion (except the last), the child is reset via ResetGracefully().
            /// This ensures the child starts fresh for each iteration, resetting variables and lifecycle state.
            ///
            /// <para><b>Difference from D.Repeat:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Repeat:</b> Infinite loop, never completes</item>
            /// <item><b>D.RepeatCount:</b> Fixed number of iterations, returns Success when done</item>
            /// </list>
            ///
            /// <para><b>Difference from D.Until:</b></para>
            /// <list type="bullet">
            /// <item><b>D.RepeatCount:</b> Repeats a fixed number of times regardless of child status</item>
            /// <item><b>D.Until:</b> Repeats until child returns a specific status or condition is met</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.RepeatCount(5);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode RepeatCount(Func<int> amount, Action lifecycle = null) => Decorator("Repeat Count", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                var _iteration = Variable(static () => 0);

                OnInvalidCheck(() => node.Child.IsInvalid());
                OnExit(ct => ExitNode(node.Child));

                OnBaseTick(() =>
                {
                    if (!node.Child.Done)
                    {
                        if (node.Child.Tick(out _, true))
                        {
                            _iteration.Value++;

                            if (_iteration.Value >= amount())
                                return Status.Success;
                        }
                    }
                    else
                        node.Child.ResetGracefully();

                    return Status.Running;
                });

                lifecycle?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that repeats its child node a specified number of times.
            /// This overload accepts a constant count value.
            /// </summary>
            /// <param name="amount">The number of times to repeat the child (constant value)</param>
            /// <param name="lifecycle">Optional lifecycle callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that repeats its child a fixed number of times</returns>
            /// <remarks>
            /// This is a convenience overload that accepts a constant integer instead of a function.
            /// See <see cref="RepeatCount(Func{int}, Action)"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode RepeatCount(int amount, Action lifecycle = null) =>
                RepeatCount(() => amount, lifecycle);
        }
    }
}

#endif