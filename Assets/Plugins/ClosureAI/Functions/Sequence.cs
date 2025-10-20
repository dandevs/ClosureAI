#if UNITASK_INSTALLED
using System;
using System.Linq;
using ClosureAI.Utilities;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a composite node that executes child nodes sequentially until one fails or all succeed.
        /// This is an "all must succeed" pattern that short-circuits on the first failure.
        /// </summary>
        /// <param name="name">The name of the sequence node for debugging and visualization</param>
        /// <param name="setup">A lambda where child nodes are declared in order and added to this sequence</param>
        /// <returns>A composite node that sequences children in order</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Executes children sequentially in the order they were declared</item>
        /// <item>Returns <see cref="Status.Success"/> only if all children succeed</item>
        /// <item>Returns <see cref="Status.Failure"/> as soon as any child fails (short-circuits)</item>
        /// <item>Returns <see cref="Status.Running"/> while the current child is running</item>
        /// <item>Skips remaining children after a failure</item>
        /// <item>Returns Success immediately if no children exist (empty sequence)</item>
        /// <item>Exits children in reverse order when the sequence exits</item>
        /// </list>
        ///
        /// <para><b>Reactive Behavior (when marked with Reactive):</b></para>
        /// <list type="bullet">
        /// <item>Checks all previously completed children for invalidation each tick</item>
        /// <item>If a previous child invalidates, resets all subsequent children gracefully</item>
        /// <item>Restarts execution from the invalidated child (with allowReEnter=true)</item>
        /// <item>This allows dynamic re-evaluation when conditions change (see CLOSUREAI_CONTEXT.md for details)</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Sequential tasks (e.g., "open door → enter room → close door")</item>
        /// <item>Multi-step actions (e.g., "aim → charge → fire → reload")</item>
        /// <item>Guarded behaviors (e.g., "check ammo → check line of sight → shoot")</item>
        /// <item>Initialization sequences (e.g., "load data → validate → initialize")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Reactive * Sequence("Attack Enemy", () =>
        /// {
        ///     Condition("Has Target", () => target != null);
        ///     Condition("In Range", () => Vector3.Distance(transform.position, target.position) &lt; attackRange);
        ///     Do("Perform Attack", () => Attack(target));
        ///     Wait("Attack Cooldown", 1.5f);
        /// });
        /// // All steps must succeed in order. If target becomes null or moves out of range,
        /// // the reactive system will reset and restart from the invalidated condition.
        /// </code>
        ///
        /// <para><b>Difference from Selector:</b></para>
        /// Sequence requires ALL children to succeed and fails on first failure.
        /// Selector requires ANY child to succeed and succeeds on first success.
        ///
        /// <para><b>Difference from SequenceAlways:</b></para>
        /// Sequence stops on first failure. SequenceAlways continues executing all children regardless of failures.
        /// </remarks>
        public static CompositeNode Sequence(string name, Action setup) => Composite("Sequence", () =>
        {
            var node = (CompositeNode)CurrentNode;
            var _currentIndex = Variable(static () => 0);

            SetNodeName(name);

            OnExit(ct => ExitNodesSequential(node.Children, node.Children.Count - 1, 0));

            OnInvalidCheck(() =>
            {
                if (node.Children.AnyInvalidToIndex(_currentIndex.Value, out var index))
                {
                    _currentIndex.SetValueSilently(index);
                    return true;
                }

                return false;
            });

            OnBaseTick(() =>
            {
                if (node.Children.Count == 0)
                    return Status.Success;

                // Trigger invalidate check within the tree
                if (node.IsReactive)
                {
                    for (var i = 0; i < _currentIndex.Value; i++)
                    {
                        var child = node.Children[i];

                        if (!child.IsInvalid() || !child.Done)
                            continue;

                        for (var j = node.Children.Count - 1; j > i; j--)
                        {
                            if (!node.Children[j].ResetGracefully())
                                return Status.Running;
                        }

                        _currentIndex.SetValueSilently(i);
                        break;
                    }

                    // Move forward with the sequence by ticking child nodes
                    while (_currentIndex.Value < node.Children.Count)
                    {
                        if (node.Children[_currentIndex.Value].Tick(out var status, true))
                        {
                            if (status == Status.Failure)
                                return Status.Failure;

                            if (_currentIndex.Value >= node.Children.Count - 1)
                                return Status.Success;

                            _currentIndex.SetValueSilently(_currentIndex.Value + 1);
                        }
                        else
                            return Status.Running;
                    }
                }
                else
                {
                    // Not reactive: Just tick forward
                    for (var j = node.Children.Count - 1; j > _currentIndex.Value; j--)
                    {
                        if (!node.Children[j].ResetGracefully())
                            return Status.Running;
                    }

                    while (node.Children[_currentIndex.Value].Tick(out var status, true))
                    {
                        if (status == Status.Failure)
                            return Status.Failure;

                        if (_currentIndex.Value >= node.Children.Count - 1)
                            return Status.Success;

                        _currentIndex.SetValueSilently(_currentIndex.Value + 1);
                    }
                }

                return Status.Running;
            });

            setup();
        });

        /// <inheritdoc cref="Sequence(string, Action)"/>
        public static CompositeNode Sequence(Action setup) => Sequence("Sequence", setup);
    }
}

#endif
