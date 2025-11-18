#if UNITASK_INSTALLED
using System;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a composite node that executes all child nodes in parallel and succeeds as soon as any child succeeds.
        /// This is a "first-to-succeed wins" pattern, useful for implementing alternatives that race against each other.
        /// </summary>
        /// <param name="name">The name of the race node for debugging and visualization</param>
        /// <param name="setup">A lambda where child nodes are declared and added to this race node</param>
        /// <returns>A composite node that races all children to completion</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Ticks all children every tick simultaneously</item>
        /// <item>Returns <see cref="Status.Success"/> as soon as any child succeeds (wins the race)</item>
        /// <item>Returns <see cref="Status.Failure"/> only when all children have failed</item>
        /// <item>Returns <see cref="Status.Running"/> while at least one child is still running</item>
        /// <item>Re-enters children that completed but are now invalid</item>
        /// <item>Exits all children in parallel when the race node exits</item>
        /// <item>Invalidates if the previously successful child invalidates, or if any other child invalidates when the winner failed</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Alternative win conditions (e.g., "reach goal OR timer expires OR player gives up")</item>
        /// <item>Interrupt patterns (e.g., "patrol UNTIL enemy spotted")</item>
        /// <item>Timeout alternatives (e.g., "complete task OR timeout")</item>
        /// <item>First-response patterns (e.g., "first enemy to enter range gets targeted")</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Race(() =>
        /// {
        ///     Sequence("Complete Mission", () =>
        ///     {
        ///         MoveTo(() => objective);
        ///         Do(() => CompleteObjective());
        ///     });
        ///     WaitUntil("Enemy Spotted", () => enemyDetected); // Wins if enemy spotted first
        ///     Wait("Timeout", 30f); // Wins after 30 seconds
        /// });
        /// // Succeeds with whichever child succeeds first
        /// </code>
        ///
        /// <para><b>Difference from Parallel:</b></para>
        /// Parallel waits for ALL children to complete, while Race succeeds as soon as ANY child succeeds.
        ///
        /// <para><b>Difference from Selector:</b></para>
        /// Selector tries children sequentially one-at-a-time, while Race runs all children simultaneously
        /// and takes the first success.
        /// </remarks>
        public static CompositeNode Race(string name, Action setup) => Composite("Race", () =>
        {
            var node = (CompositeNode)CurrentNode;
            var _lastCheckedIndex = Variable(static () => 0);

            SetNodeName(name);
            OnExit(ct => ExitNodesParallel(node.Children, 0, node.Children.Count - 1));
            OnInvalidCheck(() =>
            {
                var child = node.Children[_lastCheckedIndex.Value];

                if (child.IsInvalid())
                    return true;

                if (child.Status == Status.Failure)
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (i == _lastCheckedIndex.Value)
                            continue;

                        if (node.Children[i].IsInvalid())
                            return true;
                    }
                }

                return false;
            });

            OnBaseTick(() =>
            {
                var children = node.Children;
                var doneCount = 0;

                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    _lastCheckedIndex.SetValueSilently(i);

                    if (child.Done)
                    {
                        if (child.IsInvalid())
                        {
                            if (child.Tick(out var status, true))
                            {
                                if (status == Status.Success)
                                    return Status.Success;
                            }
                        }
                        else if (child.Status == Status.Success)
                            return Status.Success;
                        else
                            doneCount++;
                    }
                    else if (children[i].Tick(out var status))
                    {
                        if (status == Status.Success)
                            return Status.Success;

                        doneCount++;
                    }
                }

                if (doneCount >= children.Count)
                    return Status.Failure;

                return Status.Running;
            });

            setup.Invoke();
        });

        /// <summary>
        /// Creates a composite node that executes all child nodes in parallel and succeeds as soon as any child succeeds.
        /// Uses "Race" as the default name.
        /// </summary>
        /// <param name="setup">A lambda where child nodes are declared and added to this race node</param>
        /// <returns>A composite node that races all children to completion</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Race" as the default node name.
        /// See <see cref="Race(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static CompositeNode Race(Action setup) => Race("Race", setup);
    }
}

#endif
