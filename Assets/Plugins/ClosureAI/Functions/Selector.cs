#if UNITASK_INSTALLED
using System;
using ClosureAI.Utilities;
using UnityEngine;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a composite node that executes child nodes sequentially until one succeeds.
        /// This is a "try until success" pattern that short-circuits on the first successful child.
        /// </summary>
        /// <param name="name">The name of the selector node for debugging and visualization</param>
        /// <param name="setup">A lambda where child nodes are declared in order and added to this selector</param>
        /// <returns>A composite node that selects the first successful child</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Executes children sequentially in the order they were declared</item>
        /// <item>Returns <see cref="Status.Success"/> as soon as any child succeeds (short-circuits)</item>
        /// <item>Returns <see cref="Status.Failure"/> only if all children fail</item>
        /// <item>Returns <see cref="Status.Running"/> while the current child is running</item>
        /// <item>Skips remaining children after a success</item>
        /// <item>Exits children in reverse order when the selector exits</item>
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
        /// <item>Fallback chains (e.g., "try melee attack OR ranged attack OR flee")</item>
        /// <item>Priority selection (e.g., "attack visible enemy OR patrol OR idle")</item>
        /// <item>Conditional branching (e.g., "if has ammo: shoot, else: reload, else: find ammo")</item>
        /// <item>Error recovery (e.g., "try primary method OR try fallback OR fail gracefully")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Selector("Acquire Item", () =>
        /// {
        ///     Sequence("Pick Up", () =>
        ///     {
        ///         Condition(() => itemNearby);
        ///         Do(() => PickUpItem());
        ///     });
        ///     Sequence("Craft", () =>
        ///     {
        ///         Condition(() => canCraft);
        ///         Do(() => CraftItem());
        ///     });
        ///     Do("Buy", () => BuyItem()); // Last resort
        /// });
        /// // Tries to pick up first, if fails tries crafting, if fails buys
        /// </code>
        ///
        /// <para><b>Difference from Sequence:</b></para>
        /// Sequence requires ALL children to succeed and fails on first failure.
        /// Selector requires ANY child to succeed and succeeds on first success.
        /// </remarks>
        public static CompositeNode Selector(string name, Action setup) => Composite("Selector", () =>
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
                    return Status.Failure;
                }

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
                            if (status == Status.Success)
                                return Status.Success;

                            if (_currentIndex.Value >= node.Children.Count - 1)
                                return Status.Failure;

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
                        if (status == Status.Success)
                            return Status.Success;

                        if (_currentIndex.Value >= node.Children.Count - 1)
                            return Status.Failure;

                        _currentIndex.SetValueSilently(_currentIndex.Value + 1);
                    }
                }

                return Status.Running;
            });

            setup();
        });

        /// <inheritdoc cref="Selector(string, Action)"/>
        public static CompositeNode Selector(Action setup) => Selector("Selector", setup);
    }
}

#endif
