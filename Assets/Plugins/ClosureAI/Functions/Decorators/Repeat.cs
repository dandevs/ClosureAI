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
            /// Creates a decorator that repeats its child node indefinitely.
            /// The child is automatically reset and rerun each time it completes, creating an infinite loop.
            /// </summary>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that repeats its child infinitely</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child with allowReEnter=true</item>
            /// <item>When child completes (Success or Failure), immediately resets it via ResetGracefully(false)</item>
            /// <item>Always returns <see cref="Status.Running"/> (never completes on its own)</item>
            /// <item>Child's completion status is ignored - just resets and continues</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Infinite patrol loops</item>
            /// <item>Continuous monitoring behaviors</item>
            /// <item>Idle animations that loop forever</item>
            /// <item>Background tasks that never stop</item>
            /// <item>Repeating actions until externally interrupted</item>
            /// </list>
            ///
            /// <para><b>Example - Infinite Patrol:</b></para>
            /// <code>
            /// D.Repeat();
            /// Sequence("Patrol Loop", () =>
            /// {
            ///     MoveTo(() => waypoint1);
            ///     Wait(2f);
            ///     MoveTo(() => waypoint2);
            ///     Wait(2f);
            ///     // Repeats forever: waypoint1 ? wait ? waypoint2 ? wait ? waypoint1...
            /// });
            /// </code>
            ///
            /// <para><b>Example - Continuous Scanning:</b></para>
            /// <code>
            /// Race(() =>
            /// {
            ///     D.Repeat();
            ///     Leaf("Scan", () =>
            ///     {
            ///         OnBaseTick(() =>
            ///         {
            ///             ScanForEnemies();
            ///             return Status.Success; // Completes immediately, gets reset and repeats
            ///         });
            ///     });
            ///
            ///     WaitUntil(() => enemyDetected); // Wins the race when enemy found
            /// });
            /// </code>
            ///
            /// <para><b>Example - Idle Animation Loop:</b></para>
            /// <code>
            /// D.Repeat();
            /// Sequence("Idle", () =>
            /// {
            ///     Do(() => PlayAnimation("Idle"));
            ///     Wait(() => animationDuration);
            ///     // Loops the idle animation forever
            /// });
            /// </code>
            ///
            /// <para><b>Stopping Infinite Loops:</b></para>
            /// Since Repeat always returns Running, it can only be stopped by:
            /// <list type="bullet">
            /// <item>Parent node exiting/resetting (e.g., Selector trying next option)</item>
            /// <item>Using with Race or other composite that can interrupt</item>
            /// <item>Using D.Until to add a termination condition</item>
            /// <item>External reset of the behavior tree</item>
            /// </list>
            ///
            /// <para><b>Example - Stoppable Repeat:</b></para>
            /// <code>
            /// D.Until(() => shouldStop); // Stops when condition becomes true
            /// D.Repeat();
            /// DoWork();
            /// // Repeats DoWork until shouldStop becomes true
            /// </code>
            ///
            /// <para><b>Difference from D.RepeatCount:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Repeat:</b> Infinite loop, always returns Running</item>
            /// <item><b>D.RepeatCount:</b> Fixed number of iterations, returns Success when count reached</item>
            /// </list>
            ///
            /// <para><b>Difference from YieldLoop:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Repeat:</b> Decorator that wraps a child node</item>
            /// <item><b>YieldLoop:</b> Composite that yields different nodes dynamically</item>
            /// </list>
            ///
            /// <para><b>Technical Note:</b></para>
            /// ResetGracefully(false) is called with `fireInvalidations: false` to prevent unnecessary
            /// invalidation signals during the reset, since the child is immediately re-entered.
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Repeat();
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Repeat(Action setup = null) => Decorator("Repeat", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                OnInvalidCheck(() => node.Child.IsInvalid());
                OnExit(ct => ExitNode(node.Child));

                OnBaseTick(() =>
                {
                    if (node.Child.Tick(out _, true))
                        node.Child.ResetGracefully(false);

                    return Status.Running;
                });

                setup?.Invoke();
            });
        }
    }
}

#endif