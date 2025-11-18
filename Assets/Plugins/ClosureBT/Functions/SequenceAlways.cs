#if UNITASK_INSTALLED
using System;
using ClosureBT.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a composite node that executes all child nodes sequentially regardless of their success or failure.
        /// Unlike regular Sequence, this node always continues to the next child even if one fails.
        /// </summary>
        /// <param name="name">The name of the sequence node for debugging and visualization</param>
        /// <param name="setup">A lambda where child nodes are declared in order and added to this sequence</param>
        /// <returns>A composite node that always executes all children sequentially</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Executes children sequentially in the order they were declared</item>
        /// <item>ALWAYS continues to the next child, even if one fails</item>
        /// <item>Returns <see cref="Status.Success"/> only if ALL children succeeded</item>
        /// <item>Returns <see cref="Status.Success"/> after all children complete (even if some failed)</item>
        /// <item>Returns <see cref="Status.Running"/> while children are still executing</item>
        /// <item>Does NOT short-circuit on failure (key difference from regular Sequence)</item>
        /// <item>Returns Success immediately if no children exist (empty sequence)</item>
        /// <item>Exits children in reverse order when the sequence exits</item>
        /// </list>
        ///
        /// <para><b>Reactive Behavior (when marked with Reactive):</b></para>
        /// <list type="bullet">
        /// <item>Checks all previously completed children for invalidation each tick</item>
        /// <item>If a previous child invalidates, resets all subsequent children gracefully</item>
        /// <item>Restarts execution from the invalidated child (with allowReEnter=true)</item>
        /// <item>This allows dynamic re-evaluation when conditions change</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Cleanup sequences where all steps must run (e.g., "stop movement → clear target → reset state")</item>
        /// <item>Initialization lists where failures are acceptable (e.g., "load config → load optional data → start")</item>
        /// <item>Multi-step processes where partial completion is okay (e.g., "try upgrade A → try upgrade B → try upgrade C")</item>
        /// <item>Best-effort task lists (e.g., "optional task 1 → optional task 2 → required task")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// SequenceAlways("Cleanup", () =>
        /// {
        ///     Do("Stop Movement", () => StopMoving());        // Runs even if this fails
        ///     Do("Clear Target", () => target = null);        // Runs even if previous failed
        ///     Do("Reset Animation", () => ResetAnimator());   // Runs even if previous failed
        ///     Do("Play Idle", () => PlayIdleAnimation());     // Runs even if previous failed
        /// });
        /// // All cleanup steps execute regardless of individual failures
        /// </code>
        ///
        /// <para><b>Difference from Sequence:</b></para>
        /// Regular Sequence stops and fails immediately when a child fails.
        /// SequenceAlways continues executing all children even if some fail, only succeeding if all succeeded.
        ///
        /// <para><b>Use Case Example - Optional Upgrades:</b></para>
        /// <code>
        /// SequenceAlways("Apply Upgrades", () =>
        /// {
        ///     Condition("Has Speed Upgrade", () => hasSpeedUpgrade);  // Might fail
        ///     Condition("Has Damage Upgrade", () => hasDamageUpgrade); // Might fail
        ///     Condition("Has Health Upgrade", () => hasHealthUpgrade); // Might fail
        /// });
        /// // Checks all upgrade conditions. Succeeds only if player has all upgrades,
        /// // but evaluates all of them regardless.
        /// </code>
        /// </remarks>
        public static CompositeNode SequenceAlways(string name, Action setup) => Composite("Sequence Always", () =>
        {
            var node = (CompositeNode)CurrentNode;
            var _currentIndex = Variable(static () => 0);

            SetNodeName(name);
            OnEnter(() =>
            {
                for (var i = node.Children.Count - 1; i > _currentIndex.Value; i--)
                    node.Children[i].ResetImmediately();
            });
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
                {
                    Debug.LogWarning($"Node {node.Name} has no children");
                    return Status.Success;
                }

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

                    while (_currentIndex.Value < node.Children.Count)
                    {
                        if (node.Children[_currentIndex.Value].Tick(out _, true))
                        {
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
                    for (var j = node.Children.Count - 1; j > _currentIndex.Value; j--)
                    {
                        if (!node.Children[j].ResetGracefully())
                            return Status.Running;
                    }

                    while (node.Children[_currentIndex.Value].Tick(out _, true))
                    {
                        if (_currentIndex.Value >= node.Children.Count - 1)
                            return Status.Success;

                        _currentIndex.SetValueSilently(_currentIndex.Value + 1);
                    }
                }

                return Status.Running;
            });

            setup();
        });

        /// <inheritdoc cref="SequenceStar"/>
        public static CompositeNode SequenceAlways(Action setup) => SequenceAlways("Sequence Always", setup);
    }
}

#endif
