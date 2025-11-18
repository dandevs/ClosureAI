#if UNITASK_INSTALLED
using System;
using ClosureBT.Utilities;

namespace ClosureBT
{
    public static partial class BT
    {
        /// <summary>
        /// Creates a composite node that executes all child nodes simultaneously in parallel.
        /// Returns Success only when all children have completed successfully.
        /// </summary>
        /// <param name="name">The name of the parallel node for debugging and visualization</param>
        /// <param name="setup">A lambda where child nodes are declared and added to this parallel node</param>
        /// <returns>A composite node that runs all children in parallel</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Ticks all children every tick, regardless of their status</item>
        /// <item>Re-enters children that have completed but are now invalid (reactive behavior)</item>
        /// <item>Returns <see cref="Status.Running"/> while any child is still running</item>
        /// <item>Returns <see cref="Status.Success"/> only when all children are Done</item>
        /// <item>Exits all children in parallel when the parallel node exits</item>
        /// <item>Invalidates when any child invalidates</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Running multiple independent behaviors simultaneously (e.g., "move AND rotate AND play animation")</item>
        /// <item>Waiting for multiple conditions to all be true (e.g., "all enemies defeated AND timer expired")</item>
        /// <item>Coordinating concurrent tasks that must all complete</item>
        /// <item>Multi-threaded behavior execution (though actual threading depends on child implementations)</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Parallel(() =>
        /// {
        ///     WaitUntil("Reached Position", () => Vector3.Distance(transform.position, target) &lt; 0.1f);
        ///     WaitUntil("Finished Animation", () => !animator.IsPlaying("Move"));
        ///     Wait("Minimum Duration", 2f);
        /// });
        /// // Succeeds only when position reached AND animation finished AND 2 seconds elapsed
        /// </code>
        ///
        /// <para><b>Difference from Sequence:</b></para>
        /// Unlike Sequence which runs children one-at-a-time sequentially, Parallel runs all children
        /// every tick simultaneously and waits for all to complete.
        ///
        /// <para><b>Reactive Behavior:</b></para>
        /// When a child that was previously Done becomes invalid (e.g., condition changed),
        /// it will be re-entered and run again alongside other children.
        /// </remarks>
        public static CompositeNode Parallel(string name, Action setup) => Composite("Parallel", () =>
        {
            var node = (CompositeNode)CurrentNode;

            SetNodeName(name);
            OnExit(ct => ExitNodesParallel(node.Children, 0, node.Children.Count - 1));
            OnInvalidCheck(() => node.Children.AnyInvalid(out _));

            OnBaseTick(() =>
            {
                var allDone = true;

                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];

                    if (child.Done && child.IsInvalid())
                        child.Tick(out _, true);
                    else
                        child.Tick();

                    if (!child.Done)
                        allDone = false;
                }

                return allDone ? Status.Success : Status.Running;
            });

            setup();
        });

        /// <summary>
        /// Creates a composite node that executes all child nodes simultaneously in parallel.
        /// Uses "Parallel" as the default name.
        /// </summary>
        /// <param name="setup">A lambda where child nodes are declared and added to this parallel node</param>
        /// <returns>A composite node that runs all children in parallel</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Parallel" as the default node name.
        /// See <see cref="Parallel(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static CompositeNode Parallel(Action setup) => Parallel("Parallel", setup);
    }
}

#endif
