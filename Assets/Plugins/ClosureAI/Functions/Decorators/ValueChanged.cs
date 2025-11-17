#if UNITASK_INSTALLED
using System;
using System.Collections.Generic;

namespace ClosureAI
{
    public static partial class AI
    {
        public static partial class D
        {
            /// <summary>
            /// Creates a decorator that invalidates when a monitored value changes.
            /// This enables reactive behavior based on value changes rather than just boolean conditions.
            /// </summary>
            /// <typeparam name="T">The type of value to monitor for changes</typeparam>
            /// <param name="value">A function that returns the value to monitor each tick</param>
            /// <param name="setup">Optional setup callbacks to configure the decorator's behavior</param>
            /// <returns>A decorator node that tracks value changes and invalidates appropriately</returns>
            /// <remarks>
            /// <para><b>Behavior:</b></para>
            /// <list type="bullet">
            /// <item>Evaluates the value function each tick via OnInvalidCheck</item>
            /// <item>Compares current value to previous value using EqualityComparer&lt;T&gt;.Default</item>
            /// <item>Invalidates when the value changes (triggering re-evaluation in reactive trees)</item>
            /// <item>Ticks the child and returns its status</item>
            /// <item>Exits the child when this decorator exits</item>
            /// </list>
            ///
            /// <para><b>Invalidation Logic:</b></para>
            /// <list type="bullet">
            /// <item>On each OnInvalidCheck call, gets current value via value()</item>
            /// <item>Compares current to previous using EqualityComparer&lt;T&gt;.Default.Equals()</item>
            /// <item>If different, returns true (invalidates) and updates previous value</item>
            /// <item>If same, updates previous value and returns false</item>
            /// </list>
            ///
            /// <para><b>Common Use Cases:</b></para>
            /// <list type="bullet">
            /// <item>Reactive behaviors based on numeric thresholds (e.g., health changes)</item>
            /// <item>State machine transitions based on enum changes</item>
            /// <item>Target switching when target reference changes</item>
            /// <item>Re-planning when parameters change (e.g., destination changes)</item>
            /// <item>UI updates when display values change</item>
            /// </list>
            ///
            /// <para><b>Example - Health-Based Behavior:</b></para>
            /// <code>
            /// Reactive * Selector(() =>
            /// {
            ///     Sequence("Low Health", () =>
            ///     {
            ///         D.ValueChanged(() => health);
            ///         Condition(() => health &lt; 30);
            ///         // Reactive sequence will restart when health changes
            ///         Flee();
            ///     });
            ///
            ///     Sequence("High Health", () =>
            ///     {
            ///         Condition(() => health >= 30);
            ///         Attack();
            ///     });
            /// });
            /// // Switches between flee and attack when health crosses threshold
            /// </code>
            ///
            /// <para><b>Example - Target Switching:</b></para>
            /// <code>
            /// Reactive * Sequence(() =>
            /// {
            ///     D.ValueChanged(() => currentTarget);
            ///     Sequence("Engage Target", () =>
            ///     {
            ///         // When currentTarget reference changes, this sequence restarts
            ///         MoveTo(() => currentTarget.position);
            ///         AttackTarget();
            ///     });
            /// });
            /// </code>
            ///
            /// <para><b>Example - State Machine:</b></para>
            /// <code>
            /// Reactive * SequenceAlways(() =>
            /// {
            ///     D.ValueChanged(() => aiState);
            ///     YieldDynamic(controller =>
            ///     {
            ///         return _ => aiState switch
            ///         {
            ///             AIState.Idle => IdleBehavior(),
            ///             AIState.Patrol => PatrolBehavior(),
            ///             AIState.Combat => CombatBehavior(),
            ///             _ => IdleBehavior()
            ///         };
            ///     });
            ///     // When aiState changes, YieldDynamic is invalidated and restarts
            /// });
            /// </code>
            ///
            /// <para><b>Type Compatibility:</b></para>
            /// Works with any type T that implements IEquatable&lt;T&gt; or has proper Equals() override.
            /// <list type="bullet">
            /// <item>Value types (int, float, bool, enums): Direct comparison</item>
            /// <item>Reference types: Reference equality by default (unless Equals() is overridden)</item>
            /// <item>Structs with custom equality: Use custom IEquatable implementation</item>
            /// </list>
            ///
            /// <para><b>Performance Note:</b></para>
            /// The value function is called every OnInvalidCheck (every tick in reactive trees).
            /// Keep the value function lightweight. For expensive calculations, consider caching.
            ///
            /// <para><b>Difference from D.Condition:</b></para>
            /// <list type="bullet">
            /// <item><b>D.Condition:</b> Tracks boolean changes (true/false)</item>
            /// <item><b>D.ValueChanged:</b> Tracks value changes of any type T</item>
            /// </list>
            ///
            /// <para><b>Decorator Pattern Usage:</b></para>
            /// This is a decorator, so it must be placed BEFORE the node it decorates:
            /// <code>
            /// D.ValueChanged(() => myValue);
            /// MyChildNode();
            /// </code>
            /// </remarks>
            public static DecoratorNode ValueChanged<T>(Func<T> value, Action setup = null) => Decorator(
                "Value Changed", () =>
                {
                    var node = (DecoratorNode)CurrentNode;
                    var previousValue = Variable<T>();
                    var currentValue = Variable<T>();

                    OnInvalidCheck(() =>
                    {
                        currentValue.Value = value();

                        if (!EqualityComparer<T>.Default.Equals(previousValue.Value, currentValue.Value))
                        {
                            previousValue.Value = currentValue.Value;
                            return true;
                        }

                        previousValue.Value = currentValue.Value;
                        return false;
                    });

                    OnExit(ct => ExitNode(node.Child));

                    OnBaseTick(() =>
                    {
                        if (node.Child.Tick(out var status))
                            return status;
                        else
                            return Status.Running;
                    });

                    setup?.Invoke();
                });
        }
    }
}

#endif
