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
            /// Creates a decorator that repeatedly ticks its child until the child returns a specific status.
            /// This allows loops that continue until Success or Failure, depending on the target status.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="untilStatus">The target status to wait for (Success or Failure)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that repeats until the target status is reached</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Ticks the child each frame</item>
            /// <item>If child is Done and invalid, re-enters it with allowReEnter=true</item>
            /// <item>Returns the child's status when it matches the target untilStatus</item>
            /// <item>Returns <see cref="Status.Running"/> while child status doesn't match target</item>
            /// <item>Effectively loops the child until it produces the desired status</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when the child invalidates</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Retry loops (repeat until Success)</item>
            /// <item>Persistence loops (keep trying until it works)</item>
            /// <item>Failure-seeking loops (repeat until Failure to find boundary conditions)</item>
            /// <item>Polling behaviors (keep checking until condition met)</item>
            /// </list>
            ///
            /// <para><b>Example - Retry Until Success:</b></para>
            /// <code>
            /// D.Until(Status.Success);
            /// Sequence("Attempt Connection", () =>
            /// {
            ///     Condition(() => AttemptConnection());
            ///     Do(() => OnConnected());
            /// });
            /// // Keeps trying to connect until it succeeds
            /// </code>
            ///
            /// <para><b>Example - Find Until Failure:</b></para>
            /// <code>
            /// D.Until(Status.Failure);
            /// Sequence("Process Item", () =>
            /// {
            ///     Condition(() => HasMoreItems());
            ///     Do(() => ProcessNextItem());
            /// });
            /// // Processes items until HasMoreItems() returns false (Failure)
            /// </code>
            ///
            /// <para><b>Example - Combined with Timeout:</b></para>
            /// <code>
            /// Race(() =>
            /// {
            ///     D.Timeout(10f);
            ///     D.Until(Status.Success);
            ///     TryComplexOperation();
            ///     // Retries until success OR 10 seconds elapse
            /// });
            /// </code>
            ///
            /// <para><b>Re-entry Behavior:</b></para>
            /// When child is Done but invalid (e.g., reactive condition changed), it's re-entered
            /// rather than reset. This preserves the decorator's state while allowing child to update.
            ///
            /// <para><b>Difference from D.Repeat:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Repeat:</b> Infinite loop, never completes</item>
            /// <item><b>D.Until(Status):</b> Loops until child returns target status, then completes with that status</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Until(Status.Success);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Until(string name, Status untilStatus, Action setup) => Decorator($"Until", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                SetNodeName($"{name} {untilStatus}");
                OnInvalidCheck(() => node.Child.IsInvalid());
                OnExit(ct => ExitNode(node.Child));

                OnBaseTick(() =>
                {
                    var child = node.Child;
                    var status = Status.None;
                    var ok = false;

                    if (child.Done && child.IsInvalid())
                        ok = child.Tick(out status, true);
                    else
                        ok = child.Tick(out status);

                    if (ok && status == untilStatus)
                        return status;

                    return Status.Running;
                });

                setup?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that repeatedly ticks its child until the child returns a specific status.
            /// Uses "Until [Status]" as the default name.
            /// </summary>
            /// <param name="untilStatus">The target status to wait for (Success or Failure)</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that repeats until the target status is reached</returns>
            /// <remarks>
            /// This is a convenience overload that uses "Until" as the default node name.
            /// See <see cref="Until(string, Status, Action)"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode Until(Status untilStatus, Action setup = null)
            {
                return Until("Until", untilStatus, setup);
            }

            //********************************************************************************************************************

            /// <summary>
            /// Creates a decorator that runs its child until a condition becomes true, then succeeds.
            /// This is the condition-based variant of Until, useful for polling and waiting behaviors.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="condition">A function that returns the condition to check each tick</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that runs until the condition is met</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Evaluates the condition each tick</item>
            /// <item>Returns <see cref="Status.Success"/> immediately if condition is true (before ticking child)</item>
            /// <item>If condition is false, ticks the child</item>
            /// <item>If child is Done and invalid, re-enters it with allowReEnter=true</item>
            /// <item>Returns child's status if child completes AND condition becomes true</item>
            /// <item>Returns <see cref="Status.Running"/> while waiting for condition</item>
            /// <item>Exits the child when this decorator exits</item>
            /// <item>Invalidates when condition is false AND child is invalid</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Run action until external condition met (e.g., "patrol until enemy spotted")</item>
            /// <item>Background tasks with termination condition (e.g., "gather resources until full")</item>
            /// <item>Interruptible loops (e.g., "idle until input received")</item>
            /// <item>Polling with work (e.g., "process queue until empty")</item>
            /// </list>
            ///
            /// <para><b>Example - Patrol Until Enemy Spotted:</b></para>
            /// <code>
            /// D.Until(() => enemyDetected);
            /// PatrolBehavior();
            /// // Patrols until enemy detected, then succeeds
            /// </code>
            ///
            /// <para><b>Example - Gather Until Inventory Full:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.Until(() => inventory.IsFull);
            ///     Sequence("Gather Loop", () =>
            ///     {
            ///         FindResource();
            ///         CollectResource();
            ///     });
            ///
            ///     ReturnToBase();
            /// });
            /// // Keeps gathering until inventory full, then returns to base
            /// </code>
            ///
            /// <para><b>Example - Idle Until Action:</b></para>
            /// <code>
            /// Selector(() =>
            /// {
            ///     D.Until(() => actionRequested);
            ///     PlayIdleAnimation();
            ///     // Plays idle until action requested
            ///
            ///     PerformRequestedAction();
            /// });
            /// </code>
            ///
            /// <para><b>Example - Process Queue Until Empty:</b></para>
            /// <code>
            /// D.Until(() => queue.IsEmpty);
            /// D.Repeat();
            /// Sequence("Process One", () =>
            /// {
            ///     Do(() => ProcessNextQueueItem());
            ///     Wait(0.1f);
            /// });
            /// // Processes queue items with delay until queue is empty
            /// </code>
            ///
            /// <para><b>Condition Check Timing:</b></para>
            /// The condition is checked:
            /// <list type="bullet">
            /// <item>BEFORE ticking the child (early-out if already true)</item>
            /// <item>AFTER the child completes (to determine if we should return child's status)</item>
            /// </list>
            ///
            /// <para><b>Success Condition:</b></para>
            /// Returns Success when condition becomes true, regardless of child's status.
            /// The child's status is returned only if the child happens to complete in the same tick
            /// that the condition becomes true.
            ///
            /// <para><b>Difference from D.While:</b></para>
            /// <list type="bullet">
            /// <item><b>D.While:</b> Runs child WHILE condition is true, fails when false</item>
            /// <item><b>D.Until:</b> Runs child UNTIL condition becomes true, succeeds when true</item>
            /// </list>
            ///
            /// <para><b>Difference from WaitUntil:</b></para>
            /// <list type="bullet">
            /// <item><b>WaitUntil (leaf):</b> Just waits, doesn't run any child behavior</item>
            /// <item><b>D.Until (decorator):</b> Runs child behavior while waiting for condition</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Until(() => myCondition);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Until(string name, Func<bool> condition, Action setup = null) => Decorator("Until", () =>
            {
                var node = (DecoratorNode)CurrentNode;

                SetNodeName(name);
                OnExit(ct => ExitNode(node.Child));
                OnInvalidCheck(() => !condition() && node.Child.IsInvalid());
                OnBaseTick(() =>
                {
                    if (condition())
                        return Status.Success;

                    var child = node.Child;
                    var status = Status.None;
                    var ok = false;

                    if (child.Done && child.IsInvalid())
                        ok = child.Tick(out status, true);
                    else
                        ok = child.Tick(out status);

                    if (ok && condition())
                        return status;

                    return Status.Running;
                });

                setup?.Invoke();
            });

            /// <summary>
            /// Creates a decorator that runs its child until a condition becomes true, then succeeds.
            /// Uses "Until" as the default name.
            /// </summary>
            /// <param name="condition">A function that returns the condition to check each tick</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that runs until the condition is met</returns>
            /// <remarks>
            /// This is a convenience overload that uses "Until" as the default node name.
            /// See <see cref="Until(string, Func{bool}, Action)"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode Until(Func<bool> condition, Action setup = null)
            {
                return Until("Until", condition, setup);
            }
        }
    }
}

#endif
