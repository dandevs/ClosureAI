#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a conditional decorator that only runs its child when a condition is true.
            /// When the condition becomes false, the child is exited gracefully.
            /// </summary>
            /// <param name="name">The name of the decorator node for debugging and visualization</param>
            /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that conditionally executes its child</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Evaluates the condition each tick before ticking the child</item>
            /// <item>Ticks the child with allowReEnter=true when the condition is true</item>
            /// <item>Returns the child's status when the child completes while condition is true</item>
            /// <item>Exits the child and returns <see cref="Status.Failure"/> when the condition becomes false</item>
            /// <item>Returns <see cref="Status.Running"/> while child is running and condition remains true</item>
            /// <item>Invalidates when the condition value changes OR when child invalidates (if condition is true)</item>
            /// </list>
            ///
            /// <para><b>Invalidation Logic:</b></para>
            /// <list type="bullet">
            /// <item>If condition is true AND child is invalid ? decorator invalidates</item>
            /// <item>If condition changes (true?false) ? decorator invalidates</item>
            /// <item>This allows reactive parents to respond to condition changes</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Guarded behaviors that should only run under specific conditions</item>
            /// <item>Interruptible actions that stop when conditions change</item>
            /// <item>Dynamic enabling/disabling of subtrees based on state</item>
            /// <item>Conditional branching without using Sequence + Condition leaf</item>
            /// </list>
            ///
            /// <para><b>Example - Conditional Attack:</b></para>
            /// <code>
            /// Sequence(() =>
            /// {
            ///     D.Condition(() => target != null && InRange(target));
            ///     Sequence("Attack", () =>
            ///     {
            ///         // Only runs while target exists and is in range
            ///         // Automatically exits if target is lost or moves out of range
            ///         AimAt(target);
            ///         Wait(0.5f);
            ///         Fire();
            ///     });
            /// });
            /// </code>
            ///
            /// <para><b>Example - Reactive Condition in Patrol:</b></para>
            /// <code>
            /// Reactive * Sequence(() =>
            /// {
            ///     D.Condition(() => !enemyDetected);
            ///     PatrolBehavior();
            ///     // If enemyDetected becomes true, PatrolBehavior is exited and sequence fails
            ///     // Reactive sequence will detect the invalidation and re-evaluate
            /// });
            /// </code>
            ///
            /// <para><b>Difference from Condition Leaf Node:</b></para>
            /// <list type="bullet">
            /// <item><b>Condition Leaf:</b> Evaluates condition and returns Success/Failure immediately (no child)</item>
            /// <item><b>D.Condition Decorator:</b> Gates child execution based on condition, exits child when condition changes</item>
            /// </list>
            ///
            /// <para><b>Difference from D.ConditionLatch:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Condition:</b> Continuously checks condition, exits child if it becomes false</item>
            /// <item><b>D.ConditionLatch:</b> Only checks condition initially, then "latches" - child runs to completion</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.Condition(() => myCondition);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode Condition(string name, Func<bool> condition, Action setup = null) => Decorator("Condition", () =>
            {
                var node = (DecoratorNode)CurrentNode;
                var _previous = Variable(false);

                SetNodeName(name);
                OnExit(ct => ExitNode(node.Child));
                OnInvalidCheck(() =>
                {
                    var childIsInvalid = node.Child.IsInvalid();

                    if (condition() && childIsInvalid)
                        return true;

                    return condition() != _previous.Value;
                });

                OnBaseTick(() =>
                {
                    var ok = condition();
                    _previous.Value = ok;

                    if (ok)
                        return node.Child.Tick(out var status, true) ? status : Status.Running;
                    else
                        return node.Child.Exit() ? Status.Failure : Status.Running;
                });
            });

            /// <summary>
            /// Creates a conditional decorator that only runs its child when a condition is true.
            /// Uses "Condition" as the default name.
            /// </summary>
            /// <param name="condition">A function that returns the boolean condition to evaluate each tick</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that conditionally executes its child</returns>
            /// <remarks>
            /// This is a convenience overload that uses "Condition" as the default node name.
            /// See <see cref="Condition(string, Func{bool}, Action)"/> for detailed behavior description.
            /// </remarks>
            public static DecoratorNode Condition(Func<bool> condition, Action setup = null) =>
                Condition("Condition", condition, setup);
        }
    }
}

#endif