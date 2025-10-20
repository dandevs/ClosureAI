#if UNITASK_INSTALLED
using System;

namespace ClosureAI
{
    public static partial class AI
    {
        /// <summary>
        /// Creates a leaf node that executes a callback every tick and always returns Running.
        /// This node never completes on its own and will run indefinitely until externally reset.
        /// </summary>
        /// <param name="name">The name of the node for debugging and visualization</param>
        /// <param name="onTick">The callback to execute every tick via OnTick lifecycle method</param>
        /// <returns>A leaf node that continuously runs the callback</returns>
        /// <remarks>
        /// <para><b>Behavior:</b></para>
        /// <list type="bullet">
        /// <item>Executes the provided callback in the OnTick lifecycle phase (after OnBaseTick)</item>
        /// <item>Always returns <see cref="Status.Running"/> from OnBaseTick</item>
        /// <item>Never completes unless externally reset or interrupted by a parent node</item>
        /// <item>Continues running indefinitely</item>
        /// </list>
        ///
        /// <para><b>Common Use Cases:</b></para>
        /// <list type="bullet">
        /// <item>Continuous monitoring (e.g., "update distance to target")</item>
        /// <item>Per-frame updates (e.g., "apply movement", "rotate toward target")</item>
        /// <item>Background processing (e.g., "scan for enemies", "update UI")</item>
        /// <item>Placeholder behaviors that run until interrupted</item>
        /// </list>
        ///
        /// <para><b>Example:</b></para>
        /// <code>
        /// Sequence(() =>
        /// {
        ///     WaitUntil(() => enemySpotted);
        ///     JustOnTick("Track Enemy", () =>
        ///     {
        ///         transform.LookAt(enemy.position);
        ///         distanceToEnemy = Vector3.Distance(transform.position, enemy.position);
        ///     });
        /// });
        /// </code>
        ///
        /// <para><b>Note:</b></para>
        /// This node is typically used in conjunction with decorators (e.g., D.Until, D.Timeout)
        /// or composite nodes that will handle termination based on external conditions.
        /// </remarks>
        public static LeafNode JustOnTick(string name, Action onTick) => Leaf("Just On Tick", () =>
        {
            SetNodeName(name);
            OnTick(onTick);
            OnBaseTick(static () => Status.Running);
        });

        /// <summary>
        /// Creates a leaf node that executes a callback every tick and always returns Running.
        /// Uses "Just On Tick" as the default name.
        /// </summary>
        /// <param name="onTick">The callback to execute every tick via OnTick lifecycle method</param>
        /// <returns>A leaf node that continuously runs the callback</returns>
        /// <remarks>
        /// This is a convenience overload that uses "Just On Tick" as the default node name.
        /// See <see cref="JustOnTick(string, Action)"/> for detailed behavior description.
        /// </remarks>
        public static LeafNode JustOnTick(Action onTick) => JustOnTick("Just On Tick", onTick);
    }
}

#endif