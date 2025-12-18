#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that runs its child while a condition remains true, failing when the condition becomes false.
            /// Similar to D.Condition but returns Failure instead of exiting when the condition is no longer met.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
            /// <returns>A decorator node that runs while the condition is true</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Evaluates the condition each tick</item>
            /// <item>Returns <see cref="Status.Failure"/> immediately if condition is false (doesn't tick child)</item>
            /// <item>Ticks the child (with allowReEnter if Done and invalid) when condition is true</item>
            /// <item>Returns child's status when child completes while condition is true</item>
            /// <item>Returns <see cref="Status.Running"/> while child is running and condition remains true</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when condition is true AND child is invalid</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Loop-like behaviors that continue while a condition holds</item>
            /// <item>Interruptible sequences that fail fast when conditions change</item>
            /// <item>Conditional execution with failure semantics (vs. D.Condition which exits gracefully)</item>
            /// <item>Guard clauses that fail the parent if condition not met</item>
            /// </list>
            ///
            /// <para><b>Example - Loop While Condition:</b></para>
            /// <code>
            /// D.While(() => enemiesRemaining > 0);
            /// Sequence("Attack Loop", () =>
            /// {
            ///     FindNearestEnemy();
            ///     AttackEnemy();
            ///     // Keeps looping while enemies remain
            ///     // Fails immediately when enemiesRemaining reaches 0
            /// });
            /// </code>
            ///
            /// <para><b>Example - Conditional Patrol:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     Sequence("Combat", () =>
            ///     {
            ///         Condition(() => enemyDetected);
            ///         EngageCombat();
            ///     });
            ///
            ///     D.While(() => !enemyDetected);
            ///     Patrol();
            ///     // Patrol only while no enemy detected
            ///     // Fails when enemy detected, allowing selector to try combat
            /// });
            /// </code>
            ///
            /// <para><b>Example - Resource Gathering:</b></para>
            /// <code>
            /// D.While(() => inventory.HasSpace);
            /// D.Repeat();
            /// Sequence("Gather", () =>
            /// {
            ///     FindResource();
            ///     CollectResource();
            ///     // Repeats while inventory has space
            ///     // Fails when inventory full
            /// });
            /// </code>
            ///
            /// <para><b>Difference from D.Condition:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Condition:</b> Exits child gracefully (calling OnExit) when condition becomes false, returns Failure</item>
            /// <item><b>D.While:</b> Returns Failure immediately when condition is false, without first exiting child</item>
            /// </list>
            ///
            /// <para><b>Difference from WaitWhile Leaf:</b></para>
            /// <list type="bullet">
            /// <item><b>WaitWhile Leaf:</b> Waits (returns Running) while condition is true, succeeds when it becomes false</item>
            /// <item><b>D.While Decorator:</b> Runs child while condition is true, fails when it becomes false</item>
            /// </list>
            ///
            /// <para><b>Reactive Behavior:</b></para>
            /// Invalidates when condition is true and child is invalid, allowing reactive parents to
            /// restart the child when it signals invalidation.
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.While(() => myCondition);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode While(string name, Func<bool> condition, Action lifecycle = null) => Decorator("While", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                SetNodeName(name);
                OnExit(_ => ExitNode(node.Child));
                OnInvalidCheck(() => condition() && node.Child.IsInvalid());
                OnBaseTick(() =>
                {
                    if (!condition())
                        return Status.Failure;

                    var child = node.Child;
                    var status = Status.None;
                    var ok = false;

                    if (child.Done && child.IsInvalid())
                        ok = child.Tick(out status, true);
                    else
                        ok = child.Tick(out status);

                    if (ok && !condition())
                        return status;

                    return Status.Running;
                });

                lifecycle?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that runs its child while a condition remains true, failing when the condition becomes false.
            /// Uses "While" as the default name.
            /// </summary>
            /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
            /// <returns>A decorator node that runs while the condition is true</returns>
            /// <remarks>
            /// This is a convenience overload that uses "While" as the default node name.
            /// See <see cref="While(string, Func{bool})"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode While(Func<bool> condition, Action lifecycle = null)
            {
                return While("While", condition, lifecycle);
            }
        }
    }
}

#endif
